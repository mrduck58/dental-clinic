using DentalClinic.API.Application.DTOs.Schedules;
using DentalClinic.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Application.UseCases.Schedules;

/// <summary>
/// Lịch làm việc của chính nha sĩ đang đăng nhập trong một tuần (chỉ xem).
/// Lọc WorkSchedules theo tên nha sĩ (StaffName) — khớp cách DentistDashboard xác định ca.
/// </summary>
public class GetMyScheduleHandler(AppDbContext dbContext)
{
    public async Task<IEnumerable<ScheduleEntryDto>> HandleAsync(Guid userId, string weekStart, CancellationToken ct)
    {
        if (!DateOnly.TryParse(weekStart, out var start))
            throw new ArgumentException("Invalid date format. Use YYYY-MM-DD.");

        var dentist = await dbContext.Dentists.FirstOrDefaultAsync(d => d.UserId == userId, ct);
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
