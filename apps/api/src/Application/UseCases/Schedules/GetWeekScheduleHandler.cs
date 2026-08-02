using DentalClinic.API.Application.DTOs.Schedules;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Schedules;

public record GetWeekScheduleQuery(string WeekStart) : IRequest<IEnumerable<ScheduleEntryDto>>;

public class GetWeekScheduleHandler(IWorkScheduleRepository repo) : IRequestHandler<GetWeekScheduleQuery, IEnumerable<ScheduleEntryDto>>
{
    public async Task<IEnumerable<ScheduleEntryDto>> Handle(GetWeekScheduleQuery query, CancellationToken ct)
    {
        if (!DateOnly.TryParseExact(query.WeekStart, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var date))
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
