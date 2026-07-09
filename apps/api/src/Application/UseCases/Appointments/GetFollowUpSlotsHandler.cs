using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Schedules;
using Microsoft.EntityFrameworkCore;
using DentalClinic.API.Infrastructure.Persistence;

namespace DentalClinic.API.Application.UseCases.Appointments;

public record FollowUpSlotDto(
    string Time,
    bool IsBooked,
    bool IsAvailable,
    string Period);

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

        var occupiedRanges = await GetOccupiedRangesAsync(dentistId, date, ct);
        var workShifts = daySchedules.Select(ws => ws.Shift).ToHashSet();
        var slots = BuildSlots(workShifts, dentist.Shift, occupiedRanges);

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
                var occupiedRanges = dayAppointments
                    .Where(a => a.DentistId == d.Id)
                    .Select(a =>
                    {
                        var localTime = a.AppointmentDate.UtcDateTime.AddHours(7);
                        return SlotCalculator.BuildOccupiedRange(localTime.Hour, localTime.Minute, a.Service?.DurationMinutes);
                    })
                    .ToList();

                var workShifts = shiftsByName[d.FullName];
                var slots = BuildSlots(workShifts, d.Shift, occupiedRanges);

                return new DentistFollowUpSlotsDto(
                    d.Id, d.FullName, d.Specialization, string.Join(",", workShifts.OrderBy(s => s)), slots);
            })
            .Where(x => x.Slots.Count > 0)
            .OrderBy(x => x.FullName)
            .ToList();

        return new DentistsFollowUpSlotsResultDto(true, null, result);
    }

    private async Task<List<SlotCalculator.OccupiedRange>> GetOccupiedRangesAsync(
        Guid dentistId, DateOnly date, CancellationToken ct)
    {
        var dayAppointments = await appointmentRepository.GetByDateAsync(date, ct);

        // Convert thời gian đã đặt sang giờ địa phương VN (UTC+7)
        return dayAppointments
            .Where(a => a.DentistId == dentistId)
            .Select(a =>
            {
                var localTime = a.AppointmentDate.UtcDateTime.AddHours(7);
                return SlotCalculator.BuildOccupiedRange(localTime.Hour, localTime.Minute, a.Service?.DurationMinutes);
            })
            .ToList();
    }

    private static List<FollowUpSlotDto> BuildSlots(
        HashSet<string> workShifts,
        string fallbackShift,
        List<SlotCalculator.OccupiedRange> occupiedRanges)
    {
        // Khung giờ dựa trên ca làm việc THỰC TẾ trong ngày (không dùng Dentist.Shift tĩnh).
        // Dự phòng: nếu lịch trống/không xác định, dùng ca mặc định của bác sĩ.
        var shifts = workShifts.Count > 0 ? (IEnumerable<string>)workShifts : [fallbackShift];

        return SlotCalculator.AllTimes
            .Where(t => WorkShifts.IsWorkingAt(shifts, t.Hour, t.Minute))
            .Select(t =>
            {
                var timeStr = $"{t.Hour:D2}:{t.Minute:D2}";
                var slotStart = t.Hour * 60 + t.Minute;
                var slotEnd = slotStart + SlotCalculator.SlotMinutes;
                var isBooked = SlotCalculator.IsOccupied(slotStart, slotEnd, occupiedRanges);
                var period = SlotCalculator.PeriodAt(t.Hour, t.Minute);
                return new FollowUpSlotDto(timeStr, isBooked, !isBooked, period);
            }).ToList();
    }
}
