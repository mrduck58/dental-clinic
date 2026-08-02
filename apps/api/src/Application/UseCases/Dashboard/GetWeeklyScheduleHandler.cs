using DentalClinic.API.Application.DTOs.Dashboard;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static DentalClinic.API.Application.UseCases.Dashboard.DashboardDateHelper;

namespace DentalClinic.API.Application.UseCases.Dashboard;

public record GetWeeklyScheduleQuery(DateOnly? Date) : IRequest<WeeklyScheduleDto>;

public class GetWeeklyScheduleHandler(AppDbContext dbContext) : IRequestHandler<GetWeeklyScheduleQuery, WeeklyScheduleDto>
{
    public async Task<WeeklyScheduleDto> Handle(GetWeeklyScheduleQuery query, CancellationToken ct)
    {
        var today = GetVietnamToday();
        var selectedDate = query.Date ?? today;

        var dow = (int)selectedDate.DayOfWeek;
        var daysFromMonday = dow == 0 ? 6 : dow - 1;
        var weekStart = selectedDate.AddDays(-daysFromMonday);

        var week = Enumerable.Range(0, 7)
            .Select(weekStart.AddDays)
            .Select(d => new CalendarDayDto(d, d == today))
            .ToList();

        var shifts = await dbContext.WorkSchedules
            .AsNoTracking()
            .Where(s => s.Date == selectedDate && s.Type == "dentist" && !s.IsHoliday)
            .ToListAsync(ct);

        var staffNames = shifts.Select(s => s.StaffName).Distinct().ToList();

        var dentistsByName = await dbContext.Dentists
            .AsNoTracking()
            .Include(d => d.User)
            .Where(d => staffNames.Contains(d.User.FullName ?? string.Empty))
            .ToDictionaryAsync(d => d.User.FullName ?? string.Empty, d => d, ct);

        var dayStart = ToVn(selectedDate);
        var dayEnd = ToVn(selectedDate.AddDays(1));
        var busyDentistIds = (await dbContext.Appointments
                .Where(a => a.AppointmentDate >= dayStart && a.AppointmentDate < dayEnd
                            && a.Status == AppointmentStatus.InProgress)
                .Select(a => a.DentistId)
                .ToListAsync(ct))
            .ToHashSet();

        return new WeeklyScheduleDto(
            selectedDate,
            week,
            BuildShiftEntries(shifts, "morning", dentistsByName, busyDentistIds),
            BuildShiftEntries(shifts, "afternoon", dentistsByName, busyDentistIds));
    }

    private static List<ShiftEntryDto> BuildShiftEntries(
        IEnumerable<Domain.Entities.WorkSchedule> shifts,
        string shift,
        IReadOnlyDictionary<string, Domain.Entities.Dentist> dentistsByName,
        IReadOnlySet<Guid> busyDentistIds) => shifts
        .Where(s => s.Shift == shift)
        .OrderBy(s => s.StaffName)
        .Select(s =>
        {
            dentistsByName.TryGetValue(s.StaffName, out var dentist);
            var isBusy = dentist != null && busyDentistIds.Contains(dentist.Id);
            return new ShiftEntryDto(
                s.StaffName,
                dentist?.Specialization,
                dentist?.ProfilePictureUrl,
                s.Room,
                s.RoomColor,
                isBusy);
        })
        .ToList();
}
