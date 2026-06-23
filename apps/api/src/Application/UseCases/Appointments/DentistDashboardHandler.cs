using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Application.UseCases.Appointments;

public record DentistShiftDto(bool HasShift, string? Time, string? Room);
public record DentistWeekShiftsDto(int Total, int Morning, int Afternoon);

public record DentistDashboardPatientDto(
    Guid AppointmentId,
    string PatientName,
    string? ServiceName,
    string Time,
    string Status);

public record DentistDashboardResponse(
    DateOnly Date,
    int TotalPatientsToday,
    int TotalWaiting,
    int TotalInProgress,
    int TotalCompleted,
    DentistWeekShiftsDto WeekShifts,
    DentistShiftDto MorningShift,
    DentistShiftDto AfternoonShift,
    List<DentistDashboardPatientDto> UpcomingPatients);

public class DentistDashboardHandler(AppDbContext dbContext)
{
    private static readonly TimeZoneInfo VietnamTz =
        TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

    public async Task<DentistDashboardResponse> HandleAsync(Guid userId, CancellationToken ct = default)
    {
        var vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTz);
        var today = DateOnly.FromDateTime(vietnamNow);

        // Today's UTC boundaries
        var todayVnStart = new DateTimeOffset(today.Year, today.Month, today.Day, 0, 0, 0, VietnamTz.BaseUtcOffset);
        var utcStart = todayVnStart.ToUniversalTime();
        var utcEnd   = utcStart.AddDays(1);

        // Chỉ lấy data của chính bác sĩ đang đăng nhập
        var dentist = await dbContext.Dentists.FirstOrDefaultAsync(d => d.UserId == userId, ct);
        if (dentist == null)
        {
            return new DentistDashboardResponse(
                today,
                TotalPatientsToday: 0,
                TotalWaiting:       0,
                TotalInProgress:    0,
                TotalCompleted:     0,
                WeekShifts:         new DentistWeekShiftsDto(0, 0, 0),
                MorningShift:       new DentistShiftDto(false, null, null),
                AfternoonShift:     new DentistShiftDto(false, null, null),
                UpcomingPatients:   []);
        }

        // Today's appointments (all except Pending and Cancelled)
        var todayAppointments = await dbContext.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Service)
            .Where(a => a.DentistId == dentist.Id &&
                        a.AppointmentDate >= utcStart &&
                        a.AppointmentDate < utcEnd &&
                        a.Status != AppointmentStatus.Pending &&
                        a.Status != AppointmentStatus.Cancelled)
            .OrderBy(a => a.AppointmentDate)
            .ToListAsync(ct);

        int totalToday      = todayAppointments.Count(a =>
            a.Status == AppointmentStatus.CheckedIn      ||
            a.Status == AppointmentStatus.InProgress     ||
            a.Status == AppointmentStatus.PendingPayment ||
            a.Status == AppointmentStatus.Completed);
        int totalWaiting    = todayAppointments.Count(a => a.Status == AppointmentStatus.CheckedIn);
        int totalInProgress = todayAppointments.Count(a => a.Status == AppointmentStatus.InProgress);
        int totalCompleted  = todayAppointments.Count(a =>
            a.Status == AppointmentStatus.Completed ||
            a.Status == AppointmentStatus.PendingPayment);

        // This week (Monday–Sunday)
        var dow = (int)today.DayOfWeek;
        var daysFromMon = dow == 0 ? 6 : dow - 1;
        var weekStart = today.AddDays(-daysFromMon);
        var weekEnd = weekStart.AddDays(7);

        var weekSchedules = await dbContext.WorkSchedules
            .Where(s => s.StaffName == dentist.FullName &&
                        s.Date >= weekStart && s.Date < weekEnd &&
                        !s.IsHoliday)
            .ToListAsync(ct);

        var todaySchedules = weekSchedules.Where(s => s.Date == today).ToList();

        var morningEntry   = todaySchedules.FirstOrDefault(s => s.Shift == "morning");
        var afternoonEntry = todaySchedules.FirstOrDefault(s => s.Shift == "afternoon");

        var morningShift = morningEntry != null
            ? new DentistShiftDto(true, "07:30 – 12:00", morningEntry.Room)
            : new DentistShiftDto(false, null, null);

        var afternoonShift = afternoonEntry != null
            ? new DentistShiftDto(true, "13:00 – 17:30", afternoonEntry.Room)
            : new DentistShiftDto(false, null, null);

        int weekMorning   = weekSchedules.Count(s => s.Shift == "morning");
        int weekAfternoon = weekSchedules.Count(s => s.Shift == "afternoon");

        // Upcoming patients: InProgress first, then CheckedIn, then Confirmed — top 5
        var upcomingPatients = todayAppointments
            .Where(a => a.Status == AppointmentStatus.InProgress  ||
                        a.Status == AppointmentStatus.CheckedIn   ||
                        a.Status == AppointmentStatus.Confirmed)
            .Take(5)
            .Select(a =>
            {
                var vnTime = TimeZoneInfo.ConvertTime(a.AppointmentDate, VietnamTz);
                var status = a.Status switch
                {
                    AppointmentStatus.InProgress => "in_progress",
                    AppointmentStatus.CheckedIn  => "waiting",
                    _                            => "waiting"
                };
                return new DentistDashboardPatientDto(
                    a.Id,
                    a.Patient.FullName,
                    a.Service?.Name,
                    $"{vnTime.Hour:D2}:{vnTime.Minute:D2}",
                    status);
            })
            .ToList();

        return new DentistDashboardResponse(
            today,
            totalToday,
            totalWaiting,
            totalInProgress,
            totalCompleted,
            new DentistWeekShiftsDto(weekSchedules.Count, weekMorning, weekAfternoon),
            morningShift,
            afternoonShift,
            upcomingPatients);
    }
}
