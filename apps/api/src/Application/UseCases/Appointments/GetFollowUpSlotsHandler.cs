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

public record DentistFollowUpSlotsDto(
    Guid DentistId,
    string FullName,
    string Specialization,
    string Shift,
    List<FollowUpSlotDto> Slots);

public record DentistsFollowUpSlotsResultDto(
    bool HasSchedule,
    string? Message,
    List<DentistFollowUpSlotsDto> Dentists);

public class GetFollowUpSlotsHandler(AppDbContext dbContext, IAppointmentRepository appointmentRepository)
{
    private static readonly (int Hour, int Minute)[] MorningTimes = [(7, 30), (8, 30), (9, 30), (10, 30)];
    private static readonly (int Hour, int Minute)[] AfternoonTimes = [(13, 30), (14, 30), (15, 30), (16, 30)];

    /// <summary>Slots cho một bác sĩ cụ thể trong ngày.</summary>
    public async Task<FollowUpSlotsResultDto> HandleAsync(
        Guid dentistId,
        DateOnly date,
        CancellationToken ct = default)
    {
        var dentist = await dbContext.Dentists
            .FirstOrDefaultAsync(d => d.Id == dentistId, ct);

        if (dentist == null)
        {
            return new FollowUpSlotsResultDto(false, "Không tìm thấy bác sĩ.", []);
        }

        var daySchedules = await dbContext.WorkSchedules
            .Where(ws => ws.Date == date && ws.Type == "dentist" && ws.StaffName == dentist.FullName)
            .ToListAsync(ct);

        // Check for holiday
        if (daySchedules.Any(ws => ws.IsHoliday))
        {
            return new FollowUpSlotsResultDto(false, "Ngày này là ngày nghỉ lễ.", []);
        }

        // Check if dentist has work schedule for this date
        if (daySchedules.Count == 0)
        {
            var anySchedule = await dbContext.WorkSchedules.AnyAsync(ws => ws.Date == date, ct);
            if (!anySchedule && date.DayOfWeek == DayOfWeek.Sunday)
            {
                return new FollowUpSlotsResultDto(false, "Ngày Chủ Nhật không có lịch làm việc.", []);
            }
            return new FollowUpSlotsResultDto(false, "Bác sĩ không có lịch làm việc ngày này.", []);
        }

        var bookedTimes = await GetBookedTimesAsync(dentistId, date, ct);
        var workShifts = daySchedules.Select(ws => ws.Shift).ToHashSet();
        var slots = BuildSlots(workShifts, dentist.Shift, bookedTimes);

        return new FollowUpSlotsResultDto(true, null, slots);
    }

    /// <summary>Tất cả bác sĩ có lịch làm việc trong ngày kèm slots khả dụng của từng người.</summary>
    public async Task<DentistsFollowUpSlotsResultDto> HandleAllAsync(
        DateOnly date,
        CancellationToken ct = default)
    {
        var daySchedules = await dbContext.WorkSchedules
            .Where(ws => ws.Date == date && ws.Type == "dentist")
            .ToListAsync(ct);

        if (daySchedules.Any(ws => ws.IsHoliday))
        {
            return new DentistsFollowUpSlotsResultDto(false, "Ngày này là ngày nghỉ lễ.", []);
        }

        if (daySchedules.Count == 0)
        {
            var anySchedule = await dbContext.WorkSchedules.AnyAsync(ws => ws.Date == date, ct);
            if (!anySchedule && date.DayOfWeek == DayOfWeek.Sunday)
            {
                return new DentistsFollowUpSlotsResultDto(false, "Ngày Chủ Nhật không có lịch làm việc.", []);
            }
            return new DentistsFollowUpSlotsResultDto(false, "Không có bác sĩ làm việc ngày này.", []);
        }

        // Gom ca làm việc theo tên bác sĩ (một bác sĩ có thể có cả sáng lẫn chiều)
        var shiftsByName = daySchedules
            .GroupBy(ws => ws.StaffName)
            .ToDictionary(g => g.Key, g => g.Select(ws => ws.Shift).ToHashSet());

        var names = shiftsByName.Keys.ToHashSet();
        var dentists = await dbContext.Dentists
            .Where(d => names.Contains(d.FullName))
            .ToListAsync(ct);

        var dayAppointments = await appointmentRepository.GetByDateAsync(date, ct);

        var result = dentists
            .Select(d =>
            {
                var bookedTimes = dayAppointments
                    .Where(a => a.DentistId == d.Id)
                    .Select(a =>
                    {
                        var localTime = a.AppointmentDate.UtcDateTime.AddHours(7);
                        return (localTime.Hour, localTime.Minute);
                    })
                    .ToHashSet();

                var workShifts = shiftsByName[d.FullName];
                var slots = BuildSlots(workShifts, d.Shift, bookedTimes);

                return new DentistFollowUpSlotsDto(
                    d.Id, d.FullName, d.Specialization, string.Join(",", workShifts.OrderBy(s => s)), slots);
            })
            .Where(x => x.Slots.Count > 0)
            .OrderBy(x => x.FullName)
            .ToList();

        return new DentistsFollowUpSlotsResultDto(true, null, result);
    }

    private async Task<HashSet<(int Hour, int Minute)>> GetBookedTimesAsync(
        Guid dentistId, DateOnly date, CancellationToken ct)
    {
        var dayAppointments = await appointmentRepository.GetByDateAsync(date, ct);

        // Convert thời gian đã đặt sang giờ địa phương VN (UTC+7)
        return dayAppointments
            .Where(a => a.DentistId == dentistId)
            .Select(a =>
            {
                var localTime = a.AppointmentDate.UtcDateTime.AddHours(7);
                return (localTime.Hour, localTime.Minute);
            })
            .ToHashSet();
    }

    private static List<FollowUpSlotDto> BuildSlots(
        HashSet<string> workShifts,
        string fallbackShift,
        HashSet<(int Hour, int Minute)> bookedTimes)
    {
        // Khung giờ dựa trên ca làm việc THỰC TẾ trong ngày (không dùng Dentist.Shift tĩnh)
        var times = new List<(int Hour, int Minute)>();
        if (workShifts.Contains("morning")) times.AddRange(MorningTimes);
        if (workShifts.Contains("afternoon")) times.AddRange(AfternoonTimes);

        // Dự phòng: nếu Shift trong lịch trống/không xác định, dùng ca mặc định của bác sĩ
        if (times.Count == 0)
        {
            times.AddRange(fallbackShift == "afternoon" ? AfternoonTimes : MorningTimes);
        }

        return times.Select(t =>
        {
            var timeStr = $"{t.Hour:D2}:{t.Minute:D2}";
            var isBooked = bookedTimes.Contains((t.Hour, t.Minute));
            return new FollowUpSlotDto(timeStr, isBooked, !isBooked);
        }).ToList();
    }
}
