using DentalClinic.API.Application.DTOs.Schedules;
using DentalClinic.API.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Application.UseCases.Schedules;

public record GetMyScheduleQuery(Guid UserId, string WeekStart) : IRequest<IEnumerable<ScheduleEntryDto>>;

/// <summary>
/// Lịch làm việc của chính nha sĩ đang đăng nhập trong một tuần (chỉ xem).
/// Lọc WorkSchedules theo tên nha sĩ (StaffName) — khớp cách DentistDashboard xác định ca.
/// </summary>
public class GetMyScheduleHandler(AppDbContext dbContext) : IRequestHandler<GetMyScheduleQuery, IEnumerable<ScheduleEntryDto>>
{
    public async Task<IEnumerable<ScheduleEntryDto>> Handle(GetMyScheduleQuery query, CancellationToken ct)
    {
        if (!DateOnly.TryParse(query.WeekStart, out var start))
            throw new ArgumentException("Invalid date format. Use YYYY-MM-DD.");

        var dentist = await dbContext.Dentists
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.UserId == query.UserId, ct);
        if (dentist == null)
            return Enumerable.Empty<ScheduleEntryDto>();

        var end = start.AddDays(7);
        var entries = await dbContext.WorkSchedules
            .Where(s => s.StaffName == dentist.FullName && s.Date >= start && s.Date < end)
            .OrderBy(s => s.Date)
            .ToListAsync(ct);

        return entries.Select(GetWeekScheduleHandler.ToDto);
    }
}
