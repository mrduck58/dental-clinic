using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Infrastructure.Persistence.Repositories;

public class ActivityLogRepository(AppDbContext db) : IActivityLogRepository
{
    public async Task AddAsync(ActivityLog log, CancellationToken ct = default)
    {
        await db.ActivityLogs.AddAsync(log, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task<(IReadOnlyList<ActivityLog> Items, int TotalCount)> GetPagedAsync(
        string? action,
        string? module,
        string? status,
        string? search,
        DateTimeOffset? startDate,
        DateTimeOffset? endDate,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = db.ActivityLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(a => a.Action == action);

        if (!string.IsNullOrWhiteSpace(module))
            query = query.Where(a => a.Module == module);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(a => a.Status == status);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(a =>
                a.UserName.ToLower().Contains(term) ||
                a.Description.ToLower().Contains(term) ||
                (a.IpAddress != null && a.IpAddress.Contains(term)));
        }

        if (startDate.HasValue)
            query = query.Where(a => a.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(a => a.CreatedAt <= endDate.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }
}
