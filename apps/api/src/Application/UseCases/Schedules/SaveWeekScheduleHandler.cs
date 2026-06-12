using DentalClinic.API.Application.DTOs.Schedules;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;

namespace DentalClinic.API.Application.UseCases.Schedules;

public class SaveWeekScheduleHandler(IWorkScheduleRepository repo)
{
    public async Task<IEnumerable<ScheduleEntryDto>> HandleAsync(
        string weekStart, SaveWeekScheduleRequest request, CancellationToken ct)
    {
        if (!DateOnly.TryParse(weekStart, out var weekDate))
            throw new ArgumentException("Invalid date format. Use YYYY-MM-DD.");

        var entries = request.Entries.Select(e =>
        {
            if (!DateOnly.TryParse(e.Date, out var d))
                throw new ArgumentException($"Invalid entry date: {e.Date}");
            return WorkSchedule.Create(d, e.Shift, e.Type, e.Role, e.Name, e.Room, e.RoomColor, e.IsHoliday);
        }).ToList();

        await repo.ReplaceWeekAsync(weekDate, entries, ct);

        return entries.Select(GetWeekScheduleHandler.ToDto);
    }
}
