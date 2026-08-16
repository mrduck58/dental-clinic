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
        string? sortDir = null,
        CancellationToken ct = default);

    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default);
    Task<List<Notification>> GetUnreadByUserAsync(Guid userId, CancellationToken ct = default);
    Task UpdateAsync(Notification notification, CancellationToken ct = default);
    Task UpdateRangeAsync(IEnumerable<Notification> notifications, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task DeleteAllByUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Cặp (Title, RelatedEntityId) của các thông báo loại Appointment đã tồn tại của một user —
    /// dùng để chống tạo trùng nhắc lịch hẹn tự động.</summary>
    Task<IReadOnlyList<NotificationReminderKey>> GetAppointmentReminderKeysAsync(Guid userId, CancellationToken ct = default);
}

public record NotificationReminderKey(string Title, string? RelatedEntityId);
