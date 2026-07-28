using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Schedules;
using Microsoft.EntityFrameworkCore;
using DentalClinic.API.Infrastructure.Persistence;

namespace DentalClinic.API.Application.UseCases.Appointments;

public record TimeSlotDto(string Range, bool IsBooked, string Period);

public record DentistWithSlotsDto(
    Guid DentistId,
    string FullName,
    string Specialization,
    string? AvatarUrl,
    string Shift,
    int ExperienceYears,
    List<TimeSlotDto> Slots);

public class GetDentistSlotsHandler(AppDbContext dbContext, IAppointmentRepository appointmentRepository)
{
    public async Task<IEnumerable<DentistWithSlotsDto>> HandleAsync(DateOnly date, CancellationToken ct = default)
    {
        // Kiểm tra WorkSchedule cho ngày này
        var daySchedules = await dbContext.WorkSchedules
            .Where(ws => ws.Date == date)
            .ToListAsync(ct);

        // Nếu có WorkSchedule cho ngày này và mark là nghỉ lễ
        var holidaySchedule = daySchedules.FirstOrDefault(ws => ws.IsHoliday);
        if (holidaySchedule != null)
        {
            return Enumerable.Empty<DentistWithSlotsDto>();
        }

        // Nếu không có WorkSchedule nào cho ngày này và là Chủ Nhật (weekday = 7)
        if (daySchedules.Count == 0 && date.DayOfWeek == DayOfWeek.Sunday)
        {
            return Enumerable.Empty<DentistWithSlotsDto>();
        }

        // Nếu không có WorkSchedule nào cho ngày này (trừ ngày nghỉ lễ đã check ở trên)
        // → Không cho phép đặt lịch
        if (daySchedules.Count == 0)
        {
            return Enumerable.Empty<DentistWithSlotsDto>();
        }

        // Lấy WorkSchedule cho bác sĩ (type = "dentist")
        var dentistSchedules = daySchedules
            .Where(ws => ws.Type == "dentist")
            .ToList();

        // Gom các ca được phân trong ngày theo tên bác sĩ (một bác sĩ có thể có nhiều ca)
        var shiftsByName = dentistSchedules
            .GroupBy(ws => ws.StaffName)
            .ToDictionary(g => g.Key, g => g.Select(ws => ws.Shift).ToHashSet());

        // Chỉ lấy bác sĩ có trong WorkSchedule của ngày đó
        var dentistNames = dentistSchedules
            .Select(ws => ws.StaffName)
            .ToHashSet();

        var dentists = await dbContext.Dentists
            .Include(d => d.User)
            .Where(d => dentistNames.Contains(d.User.FullName ?? string.Empty))
            .ToListAsync(ct);
        var dayAppointments = await appointmentRepository.GetByDateAsync(date, ct);

        return dentists.Select(d =>
        {
            var occupiedRanges = dayAppointments
                .Where(a => a.DentistId == d.Id)
                .Select(a =>
                {
                    var localTime = a.AppointmentDate.UtcDateTime.AddHours(7);
                    return SlotCalculator.BuildOccupiedRange(localTime.Hour, localTime.Minute, a.Service?.DurationMinutes);
                })
                .ToList();

            // Khung giờ theo các ca THỰC TẾ được phân trong ngày; dự phòng dùng ca tĩnh của bác sĩ
            var assignedShifts = shiftsByName.TryGetValue(d.FullName, out var s) && s.Count > 0
                ? (IEnumerable<string>)s
                : [d.Shift];

            var slots = SlotCalculator.AllTimes
                .Where(t => WorkShifts.IsWorkingAt(assignedShifts, t.Hour, t.Minute))
                .Select(t =>
                {
                    var slotStart = t.Hour * 60 + t.Minute;
                    var slotEnd = slotStart + SlotCalculator.SlotMinutes;
                    var range = $"{t.Hour:D2}:{t.Minute:D2} - {slotEnd / 60:D2}:{slotEnd % 60:D2}";
                    var isBooked = SlotCalculator.IsOccupied(slotStart, slotEnd, occupiedRanges);
                    var period = SlotCalculator.PeriodAt(t.Hour, t.Minute);
                    return new TimeSlotDto(range, isBooked, period);
                }).ToList();

            return new DentistWithSlotsDto(
                d.Id,
                d.FullName,
                d.Specialization,
                d.ProfilePictureUrl,
                d.Shift,
                d.ExperienceYears ?? 0,
                slots);
        });
    }

    public async Task<IEnumerable<string>> GetWorkingDatesForDentistAsync(
        Guid dentistId,
        int year,
        int month,
        CancellationToken ct = default)
    {
        var dentist = await dbContext.Dentists
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.Id == dentistId || d.UserId == dentistId, ct);
        if (dentist == null) return Enumerable.Empty<string>();

        var fullName = dentist.FullName;
        var startDate = new DateOnly(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var schedules = await dbContext.WorkSchedules
            .AsNoTracking()
            .Where(ws => ws.Date >= startDate && ws.Date <= endDate)
            .ToListAsync(ct);

        var datesWithSchedules = schedules
            .Where(ws => ws.Type == "dentist" && string.Equals(ws.StaffName, fullName, StringComparison.OrdinalIgnoreCase) && !ws.IsHoliday)
            .Select(ws => ws.Date)
            .ToHashSet();

        if (datesWithSchedules.Count > 0)
        {
            return datesWithSchedules.Select(d => d.ToString("yyyy-MM-dd")).OrderBy(d => d);
        }

        var result = new List<string>();
        for (var d = startDate; d <= endDate; d = d.AddDays(1))
        {
            if (d.DayOfWeek != DayOfWeek.Sunday)
            {
                result.Add(d.ToString("yyyy-MM-dd"));
            }
        }
        return result;
    }
}
