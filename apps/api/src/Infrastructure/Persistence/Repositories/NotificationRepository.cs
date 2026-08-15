using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Infrastructure.Persistence.Repositories;

public class NotificationRepository(AppDbContext db) : INotificationRepository
{
    public async Task AddAsync(Notification notification, CancellationToken ct = default)
    {
        await db.Notifications.AddAsync(notification, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task AddRangeAsync(IEnumerable<Notification> notifications, CancellationToken ct = default)
    {
        await db.Notifications.AddRangeAsync(notifications, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task<Notification?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Notifications.FirstOrDefaultAsync(n => n.Id == id, ct);

    public async Task<(IReadOnlyList<Notification> Items, int TotalCount)> GetPagedAsync(
        Guid userId,
        string? type = null,
        string? priority = null,
        bool? isRead = null,
        string? search = null,
        int page = 1,
        int pageSize = 10,
        string? sortDir = null,
        CancellationToken ct = default)
    {
        var query = db.Notifications
            .Where(n => n.UserId == userId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(n => n.Type == type);

        if (!string.IsNullOrWhiteSpace(priority))
            query = query.Where(n => n.Priority == priority);

        if (isRead.HasValue)
            query = query.Where(n => n.IsRead == isRead.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(n =>
                EF.Functions.ILike(n.Title, term) ||
                EF.Functions.ILike(n.Body, term));
        }

        var total = await query.CountAsync(ct);
        var ordered = string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase)
            ? query.OrderBy(n => n.CreatedAt)
            : query.OrderByDescending(n => n.CreatedAt);
        var items = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default)
        => await db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead, ct);

    public async Task<List<Notification>> GetUnreadByUserAsync(Guid userId, CancellationToken ct = default)
        => await db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync(ct);

    public async Task UpdateAsync(Notification notification, CancellationToken ct = default)
    {
        db.Notifications.Update(notification);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateRangeAsync(IEnumerable<Notification> notifications, CancellationToken ct = default)
    {
        db.Notifications.UpdateRange(notifications);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var notification = await db.Notifications.FindAsync([id], ct);
        if (notification is not null)
        {
            db.Notifications.Remove(notification);
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task<IReadOnlyList<NotificationReminderKey>> GetAppointmentReminderKeysAsync(Guid userId, CancellationToken ct = default)
        => await db.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId && n.Type == NotificationType.Appointment)
            .Select(n => new NotificationReminderKey(n.Title, n.RelatedEntityId))
            .ToListAsync(ct);
}
