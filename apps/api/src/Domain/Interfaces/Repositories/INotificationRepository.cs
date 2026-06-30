using DentalClinic.API.Domain.Entities;

namespace DentalClinic.API.Domain.Interfaces.Repositories;

public interface INotificationRepository
{
    Task AddAsync(Notification notification, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<Notification> notifications, CancellationToken ct = default);
    Task<Notification?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<(IReadOnlyList<Notification> Items, int TotalCount)> GetPagedAsync(
        Guid userId,
        string? type = null,
        string? priority = null,
        bool? isRead = null,
        string? search = null,
        int page = 1,
        int pageSize = 10,
        CancellationToken ct = default);

    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default);
    Task<List<Notification>> GetUnreadByUserAsync(Guid userId, CancellationToken ct = default);
    Task UpdateAsync(Notification notification, CancellationToken ct = default);
    Task UpdateRangeAsync(IEnumerable<Notification> notifications, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
