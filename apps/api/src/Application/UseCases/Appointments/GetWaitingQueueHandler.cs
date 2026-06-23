using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Application.UseCases.Appointments;

public record QueuePatientDto(
    Guid AppointmentId,
    string AppointmentCode,
    string PatientName,
    string? PatientPhone,
    string? ServiceName,
    DateTimeOffset AppointmentDate,
    string Status,
    string? Symptoms,
    int WaitMinutes);

public record DentistQueueDto(
    Guid DentistId,
    string DentistName,
    string? RoomName,
    string DentistColor,
    List<QueuePatientDto> Patients);

public record WaitingQueueResponse(
    DateOnly Date,
    int TotalWaiting,
    int TotalInProgress,
    int TotalCompleted,
    List<DentistQueueDto> Dentists);

public class GetWaitingQueueHandler(AppDbContext dbContext)
{
    private static readonly TimeZoneInfo VietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

    public async Task<WaitingQueueResponse> HandleAsync(DateOnly date, CancellationToken ct = default)
    {
        // Convert Vietnam local date to UTC range for database query
        // Vietnam is UTC+7, so Vietnam 00:00 = UTC 17:00 previous day
        var vietnamTz = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        var vietnamDateStart = new DateTimeOffset(date.Year, date.Month, date.Day, 0, 0, 0, vietnamTz.BaseUtcOffset);
        var utcStart = vietnamDateStart.ToUniversalTime(); // Start of Vietnam date in UTC
        var utcEnd = utcStart.AddDays(1); // Start of next Vietnam date in UTC

        var nowVietnam = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vietnamTz);

        var appointments = await dbContext.Appointments
            .Include(a => a.Patient).ThenInclude(p => p.User)
            .Include(a => a.Dentist)
            .Include(a => a.Service)
            .Where(a => a.AppointmentDate >= utcStart && a.AppointmentDate < utcEnd &&
                        (a.Status == AppointmentStatus.CheckedIn ||
                         a.Status == AppointmentStatus.InProgress ||
                         a.Status == AppointmentStatus.Completed))
            .OrderBy(a => a.AppointmentDate)
            .ToListAsync(ct);

        var dateOnly = date; // Use the Vietnam date parameter directly
        var workSchedules = await dbContext.WorkSchedules
            .Where(ws => ws.Date == dateOnly)
            .ToListAsync(ct);

        var dentistGroups = appointments
            .GroupBy(a => a.DentistId)
            .Select(g =>
            {
                var dentist = g.First().Dentist;
                var dentistColor = GetDentistColor(dentist.FullName);
                var firstAppointment = g.First();

                var patients = g.Select(a => new QueuePatientDto(
                    a.Id,
                    $"DK{a.AppointmentDate:yyyyMMdd}{a.Id.ToString("N")[..6].ToUpper()}",
                    a.Patient.FullName,
                    a.Patient.User?.PhoneNumber,
                    a.Service?.Name,
                    a.AppointmentDate,
                    a.Status.ToString(),
                    a.Symptoms,
                    a.Status == AppointmentStatus.CheckedIn
                        ? (int)(nowVietnam - a.AppointmentDate.LocalDateTime).TotalMinutes
                        : 0
                )).ToList();

                var shift = firstAppointment.AppointmentDate.Hour < 12 ? "morning" : "afternoon";
                var roomName = workSchedules
                    .FirstOrDefault(ws => ws.StaffName == dentist.FullName && ws.Shift == shift)?.Room;

                return new DentistQueueDto(
                    dentist.Id,
                    dentist.FullName,
                    roomName ?? $"Phòng {(dentist.FullName.Contains("Hùng") ? "2" : "1")}",
                    dentistColor,
                    patients);
            })
            .ToList();

        return new WaitingQueueResponse(
            date,
            appointments.Count(a => a.Status == AppointmentStatus.CheckedIn),
            appointments.Count(a => a.Status == AppointmentStatus.InProgress),
            appointments.Count(a => a.Status == AppointmentStatus.Completed),
            dentistGroups);
    }

    private static string GetDentistColor(string dentistName)
    {
        return dentistName.ToLowerInvariant() switch
        {
            var n when n.Contains("thảo") => "sky",
            var n when n.Contains("minh") => "violet",
            var n when n.Contains("linh") => "rose",
            var n when n.Contains("hùng") => "amber",
            _ => "slate"
        };
    }
}
