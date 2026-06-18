using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Infrastructure.Persistence.Repositories;

public class WorkScheduleRepository(AppDbContext db) : IWorkScheduleRepository
{
    public async Task<IEnumerable<WorkSchedule>> GetByWeekAsync(DateOnly weekStart, CancellationToken ct = default)
    {
        var weekEnd = weekStart.AddDays(6);
        return await db.WorkSchedules
            .Where(s => s.Date >= weekStart && s.Date <= weekEnd)
            .OrderBy(s => s.Date)
            .ThenBy(s => s.Shift)
            .ToListAsync(ct);
    }

    public async Task ReplaceWeekAsync(DateOnly weekStart, IEnumerable<WorkSchedule> entries, CancellationToken ct = default)
    {
        var weekEnd = weekStart.AddDays(6);
        var existing = await db.WorkSchedules
            .Where(s => s.Date >= weekStart && s.Date <= weekEnd)
            .ToListAsync(ct);

        db.WorkSchedules.RemoveRange(existing);
        await db.WorkSchedules.AddRangeAsync(entries, ct);
        await db.SaveChangesAsync(ct);
    }
}
