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
        Guid? userId,
        string? action,
        string? module,
        string? status,
        string? search,
        DateTimeOffset? startDate,
        DateTimeOffset? endDate,
        int page,
        int pageSize,
        string? sortDir = null,
        string? excludeAction = null,
        CancellationToken ct = default)
    {
        var query = db.ActivityLogs.AsQueryable();

        if (userId.HasValue)
            query = query.Where(a => a.UserId == userId.Value);

        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(a => a.Action == action);

        if (!string.IsNullOrWhiteSpace(excludeAction))
            query = query.Where(a => a.Action != excludeAction);

        if (!string.IsNullOrWhiteSpace(module))
            query = query.Where(a => a.Module == module);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(a => a.Status == status);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(a =>
                EF.Functions.ILike(a.UserName, term) ||
                EF.Functions.ILike(a.Description, term) ||
                (a.IpAddress != null && EF.Functions.ILike(a.IpAddress, term)));
        }

        if (startDate.HasValue)
            query = query.Where(a => a.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(a => a.CreatedAt <= endDate.Value);

        var total = await query.CountAsync(ct);
        var ordered = string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase)
            ? query.OrderBy(a => a.CreatedAt)
            : query.OrderByDescending(a => a.CreatedAt);
        var items = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }
}
