using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace DentalClinic.API.Infrastructure.Services;

public class NotificationService(
    INotificationRepository repository,
    ILogger<NotificationService> logger) : INotificationService
{
    public async Task CreateAsync(CreateNotificationRequest request, CancellationToken ct = default)
    {
        try
        {
            var notification = Notification.Create(
                request.UserId,
                request.Type,
                request.Priority,
                request.Title,
                request.Body,
                request.RelatedEntityType,
                request.RelatedEntityId);

            await repository.AddAsync(notification, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to create notification for user {UserId}", request.UserId);
        }
    }

    public async Task CreateForMultipleUsersAsync(
        IEnumerable<Guid> userIds,
        CreateNotificationRequest template,
        CancellationToken ct = default)
    {
        try
        {
            var notifications = userIds.Select(uid => Notification.Create(
                uid,
                template.Type,
                template.Priority,
                template.Title,
                template.Body,
                template.RelatedEntityType,
                template.RelatedEntityId)).ToList();

            await repository.AddRangeAsync(notifications, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to create bulk notifications");
        }
    }

    public async Task MarkAsReadAsync(Guid notificationId, CancellationToken ct = default)
    {
        try
        {
            var notification = await repository.GetByIdAsync(notificationId, ct);
            if (notification is null) return;

            notification.MarkAsRead();
            await repository.UpdateAsync(notification, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to mark notification {Id} as read", notificationId);
        }
    }

    public async Task MarkAllAsReadAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
            var unread = await repository.GetUnreadByUserAsync(userId, ct);
            if (unread.Count == 0) return;

            foreach (var n in unread)
                n.MarkAsRead();

            await repository.UpdateRangeAsync(unread, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to mark all notifications as read for user {UserId}", userId);
        }
    }

    public async Task DeleteAsync(Guid notificationId, CancellationToken ct = default)
    {
        try
        {
            await repository.DeleteAsync(notificationId, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to delete notification {Id}", notificationId);
        }
    }
}
