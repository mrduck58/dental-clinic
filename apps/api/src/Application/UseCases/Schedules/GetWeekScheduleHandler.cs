using DentalClinic.API.Application.DTOs.Schedules;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;

namespace DentalClinic.API.Application.UseCases.Schedules;

public class GetWeekScheduleHandler(IWorkScheduleRepository repo)
{
    public async Task<IEnumerable<ScheduleEntryDto>> HandleAsync(string weekStart, CancellationToken ct)
    {
        if (!DateOnly.TryParse(weekStart, out var date))
            throw new ArgumentException("Invalid date format. Use YYYY-MM-DD.");

        var entries = await repo.GetByWeekAsync(date, ct);
        return entries.Select(ToDto);
    }

    internal static ScheduleEntryDto ToDto(WorkSchedule s) => new(
        s.Id.ToString(),
        s.Date.ToString("yyyy-MM-dd"),
        s.Shift,
        s.Type,
        s.Role,
        s.StaffName,
        s.Room,
        s.RoomColor,
        s.IsHoliday
    );
}
