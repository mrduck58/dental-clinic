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
            .Where(d => dentistNames.Contains(d.FullName))
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
                d.User.ProfilePictureUrl,
                d.Shift,
                d.ExperienceYears,
                slots);
        });
    }
}
