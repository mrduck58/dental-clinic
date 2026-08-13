using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Schedules;
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

    public async Task<IReadOnlyList<WorkSchedule>> GetDentistSchedulesForDateAsync(DateOnly date, string? room = null, CancellationToken ct = default)
    {
        var query = db.WorkSchedules
            .Where(s => s.Type == "dentist" && !s.IsHoliday && s.Date == date && WorkShifts.AllValidCodes.Contains(s.Shift));
        if (room != null)
            query = query.Where(s => s.Room == room);
        return await query.ToListAsync(ct);
    }

    public async Task<IReadOnlyList<WorkSchedule>> GetByDateAsync(DateOnly date, CancellationToken ct = default)
        => await db.WorkSchedules.Where(ws => ws.Date == date).ToListAsync(ct);

    public async Task<IReadOnlyList<WorkSchedule>> GetByDateRangeAsync(DateOnly start, DateOnly end, CancellationToken ct = default)
        => await db.WorkSchedules.AsNoTracking().Where(ws => ws.Date >= start && ws.Date <= end).ToListAsync(ct);

    public async Task<IReadOnlyList<WorkSchedule>> GetByStaffNameAndDateRangeAsync(string staffName, DateOnly start, DateOnly end, CancellationToken ct = default)
        => await db.WorkSchedules
            .Where(s => s.StaffName == staffName && s.Date >= start && s.Date < end)
            .OrderBy(s => s.Date)
            .ToListAsync(ct);
}
