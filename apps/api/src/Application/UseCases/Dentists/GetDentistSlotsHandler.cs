using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Schedules;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Dentists;

public record TimeSlotDto(string Range, bool IsBooked, string Period);

public record DentistWithSlotsDto(
    Guid DentistId,
    string FullName,
    string Specialization,
    string? AvatarUrl,
    string Shift,
    int ExperienceYears,
    List<TimeSlotDto> Slots);

public record GetDentistSlotsQuery(DateOnly Date) : IRequest<IEnumerable<DentistWithSlotsDto>>;

public class GetDentistSlotsHandler(
    IWorkScheduleRepository workScheduleRepository,
    IDentistRepository dentistRepository,
    IAppointmentRepository appointmentRepository)
    : IRequestHandler<GetDentistSlotsQuery, IEnumerable<DentistWithSlotsDto>>
{
    public async Task<IEnumerable<DentistWithSlotsDto>> Handle(GetDentistSlotsQuery request, CancellationToken ct)
    {
        var date = request.Date;

        // Kiểm tra WorkSchedule cho ngày này
        var daySchedules = await workScheduleRepository.GetByDateAsync(date, ct);

        // Nếu có WorkSchedule đánh dấu là ngày nghỉ lễ
        if (daySchedules.Any(ws => ws.IsHoliday))
        {
            return Enumerable.Empty<DentistWithSlotsDto>();
        }

        // Lấy WorkSchedule liên quan đến bác sĩ
        var dentistSchedules = daySchedules
            .Where(ws => ws.Type == "dentist" || ws.Role == "dentist" || string.Equals(ws.Type, "Khám", StringComparison.OrdinalIgnoreCase))
            .ToList();

        List<DentistProfile> dentists;
        var shiftsByName = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        if (dentistSchedules.Count > 0)
        {
            shiftsByName = dentistSchedules
                .GroupBy(ws => ws.StaffName)
                .ToDictionary(g => g.Key, g => g.Select(ws => ws.Shift).ToHashSet(), StringComparer.OrdinalIgnoreCase);

            var dentistNames = dentistSchedules
                .Select(ws => ws.StaffName)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            dentists = await dentistRepository.GetActiveByNamesAsync(dentistNames, ct);
        }
        else
        {
            // Chưa có WorkSchedule cụ thể cho bác sĩ ngày này -> Mặc định lấy tất cả bác sĩ đang hoạt động
            dentists = await dentistRepository.GetAllActiveWithUserAsync(ct);
        }

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
}
