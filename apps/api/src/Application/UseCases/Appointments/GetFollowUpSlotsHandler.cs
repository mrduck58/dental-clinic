using DentalClinic.API.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using DentalClinic.API.Infrastructure.Persistence;

namespace DentalClinic.API.Application.UseCases.Appointments;

public record FollowUpSlotDto(
    string Time,
    bool IsBooked,
    bool IsAvailable);

public record FollowUpSlotsResultDto(
    bool HasSchedule,
    string? Message,
    List<FollowUpSlotDto> Slots);

public class GetFollowUpSlotsHandler(AppDbContext dbContext, IAppointmentRepository appointmentRepository)
{
    private static readonly (int Hour, int Minute)[] MorningTimes = [(7, 30), (8, 30), (9, 30), (10, 30)];
    private static readonly (int Hour, int Minute)[] AfternoonTimes = [(13, 30), (14, 30), (15, 30), (16, 30)];

    public async Task<FollowUpSlotsResultDto> HandleAsync(
        Guid dentistId,
        DateOnly date,
        CancellationToken ct = default)
    {
        // Get dentist info first
        var dentist = await dbContext.Dentists
            .FirstOrDefaultAsync(d => d.Id == dentistId, ct);

        if (dentist == null)
        {
            return new FollowUpSlotsResultDto(false, "Không tìm thấy bác sĩ.", []);
        }

        // Check if there's a work schedule for this date and this dentist
        var daySchedules = await dbContext.WorkSchedules
            .Where(ws => ws.Date == date && ws.Type == "dentist" && ws.StaffName == dentist.FullName)
            .ToListAsync(ct);

        Console.WriteLine($"[DEBUG] Date: {date}, Dentist: {dentist.FullName}, Schedules found: {daySchedules.Count}");
        foreach (var s in daySchedules)
        {
            Console.WriteLine($"  Schedule: Type={s.Type}, StaffName={s.StaffName}, Shift={s.Shift}, IsHoliday={s.IsHoliday}");
        }

        // Check for holiday
        var holidaySchedule = daySchedules.FirstOrDefault(ws => ws.IsHoliday);
        if (holidaySchedule != null)
        {
            return new FollowUpSlotsResultDto(false, "Ngày này là ngày nghỉ lễ.", []);
        }

        // Check if dentist has work schedule for this date
        if (daySchedules.Count == 0)
        {
            // Check if there's any schedule for this date (might be a day off)
            var anySchedule = await dbContext.WorkSchedules
                .AnyAsync(ws => ws.Date == date, ct);
            
            if (!anySchedule && date.DayOfWeek == DayOfWeek.Sunday)
            {
                return new FollowUpSlotsResultDto(false, "Ngày Chủ Nhật không có lịch làm việc.", []);
            }
            
            return new FollowUpSlotsResultDto(false, "Bác sĩ không có lịch làm việc ngày này.", []);
        }

        // Get appointments for this date (local time)
        var dayAppointments = await appointmentRepository.GetByDateAsync(date, ct);
        Console.WriteLine($"[DEBUG] Appointments on {date}: {dayAppointments.Count}, for this dentist: {dayAppointments.Count(a => a.DentistId == dentistId)}");

        // Get booked times for this dentist (convert to Vietnam local time UTC+7)
        var bookedTimes = dayAppointments
            .Where(a => a.DentistId == dentistId)
            .Select(a => {
                var utcTime = a.AppointmentDate.UtcDateTime;
                var localTime = utcTime.AddHours(7);
                Console.WriteLine($"  Booked: {a.AppointmentDate} UTC -> {localTime} local (Hour={localTime.Hour}, Min={localTime.Minute})");
                return (localTime.Hour, localTime.Minute);
            })
            .ToHashSet();

        // Determine time slots based on dentist's shift
        var times = dentist.Shift == "afternoon" ? AfternoonTimes : MorningTimes;

        var slots = times.Select(t =>
        {
            var timeStr = $"{t.Hour:D2}:{t.Minute:D2}";
            var isBooked = bookedTimes.Contains((t.Hour, t.Minute));
            return new FollowUpSlotDto(timeStr, isBooked, !isBooked);
        }).ToList();

        return new FollowUpSlotsResultDto(true, null, slots);
    }
}
