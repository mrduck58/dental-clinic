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

    public async Task RemoveRangeAsync(IEnumerable<WorkSchedule> entries, CancellationToken ct = default)
    {
        var ids = entries.Select(e => e.Id).ToList();
        if (ids.Count == 0) return;

        // Nạp lại bản có tracking rồi mới xóa: entry truyền vào thường đến từ GetByDateRangeAsync
        // (AsNoTracking), xóa thẳng thực thể rời sẽ phụ thuộc vào hành vi tự attach của EF.
        var tracked = await db.WorkSchedules.Where(s => ids.Contains(s.Id)).ToListAsync(ct);
        if (tracked.Count == 0) return;

        db.WorkSchedules.RemoveRange(tracked);
        await db.SaveChangesAsync(ct);
    }
}
