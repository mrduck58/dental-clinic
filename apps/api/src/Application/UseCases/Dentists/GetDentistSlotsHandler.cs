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
            .Where(ws => (ws.Type == "dentist" || ws.Role == "dentist" || string.Equals(ws.Type, "Khám", StringComparison.OrdinalIgnoreCase))
                         && !ws.IsHoliday)
            .ToList();

        var allActiveDentists = await dentistRepository.GetAllActiveWithUserAsync(ct);
        var dayAppointments = await appointmentRepository.GetByDateAsync(date, ct);

        List<(DentistProfile Dentist, List<string> AssignedShifts)> targetDentists;

        if (dentistSchedules.Count > 0)
        {
            targetDentists = [];
            foreach (var d in allActiveDentists)
            {
                var matchingSchedules = dentistSchedules
                    .Where(ws => (ws.EmployeeId.HasValue && ws.EmployeeId.Value == d.EmployeeId)
                                 || StaffNameMatcher.IsSamePerson(ws.StaffName, d.FullName)
                                 || StaffNameMatcher.IsSamePerson(ws.StaffName, d.Employee?.User?.FullName))
                    .ToList();

                if (matchingSchedules.Count > 0)
                {
                    var shifts = matchingSchedules
                        .Select(ws => ws.Shift)
                        .Where(s => !string.IsNullOrWhiteSpace(s) && !string.Equals(s, "Off", StringComparison.OrdinalIgnoreCase) && !string.Equals(s, "Nghỉ", StringComparison.OrdinalIgnoreCase))
                        .Distinct()
                        .ToList();

                    if (shifts.Count > 0)
                    {
                        targetDentists.Add((d, shifts));
                    }
                }
            }
        }
        else
        {
            // Chưa có WorkSchedule cụ thể cho bác sĩ ngày này -> Dùng ca mặc định của từng bác sĩ
            targetDentists = allActiveDentists
                .Select(d => (d, new List<string> { d.Shift }))
                .ToList();
        }

        return targetDentists.Select(item =>
        {
            var d = item.Dentist;
            var assignedShifts = item.AssignedShifts;

            var occupiedRanges = dayAppointments
                .Where(a => a.DentistId == d.Id)
                .Select(a =>
                {
                    var localTime = a.AppointmentDate.UtcDateTime.AddHours(7);
                    return SlotCalculator.BuildOccupiedRange(localTime.Hour, localTime.Minute, a.Service?.DurationMinutes, a.Status);
                })
                .ToList();

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
