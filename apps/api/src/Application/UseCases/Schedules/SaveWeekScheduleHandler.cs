using DentalClinic.API.Application.DTOs.Schedules;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Schedules;

public record SaveWeekScheduleCommand(string WeekStart, SaveWeekScheduleRequest Request) : IRequest<IEnumerable<ScheduleEntryDto>>;

public class SaveWeekScheduleHandler(IWorkScheduleRepository repo) : IRequestHandler<SaveWeekScheduleCommand, IEnumerable<ScheduleEntryDto>>
{
    public async Task<IEnumerable<ScheduleEntryDto>> Handle(SaveWeekScheduleCommand command, CancellationToken ct)
    {
        if (!DateOnly.TryParseExact(command.WeekStart, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var weekDate))
            throw new ArgumentException("Invalid date format. Use YYYY-MM-DD.");

        var entries = command.Request.Entries.Select(e =>
        {
            if (!DateOnly.TryParseExact(e.Date, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var d))
                throw new ArgumentException($"Invalid entry date: {e.Date}");
            return WorkSchedule.Create(d, e.Shift, e.Type, e.Role, e.Name, e.Room, e.RoomColor, e.IsHoliday);
        }).ToList();

        await repo.ReplaceWeekAsync(weekDate, entries, ct);

        return entries.Select(GetWeekScheduleHandler.ToDto);
    }
}
