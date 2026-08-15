using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace DentalClinic.API.Infrastructure.Services;

public class NotificationService(
    INotificationRepository repository,
    ILogger<NotificationService> logger,
    IFirebasePushNotificationService? pushService = null) : INotificationService
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

            // Tự động đẩy thông báo Firebase Cloud Messaging (FCM) trực tiếp đến thiết bị người dùng
            if (pushService != null)
            {
                _ = pushService.SendPushNotificationAsync(
                    request.UserId,
                    request.Title,
                    request.Body,
                    request.Type,
                    request.RelatedEntityId,
                    ct);
            }
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
            var idList = userIds.ToList();
            var notifications = idList.Select(uid => Notification.Create(
                uid,
                template.Type,
                template.Priority,
                template.Title,
                template.Body,
                template.RelatedEntityType,
                template.RelatedEntityId)).ToList();

            await repository.AddRangeAsync(notifications, ct);

            if (pushService != null)
            {
                _ = pushService.SendPushNotificationToMultipleAsync(
                    idList,
                    template.Title,
                    template.Body,
                    template.Type,
                    template.RelatedEntityId,
                    ct);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to create bulk notifications");
        }
    }

    // MarkAsReadAsync/DeleteAsync do người dùng chủ động gọi — KHÔNG nuốt lỗi như các hàm Create*
    // (vốn là side-effect fire-and-forget của luồng nghiệp vụ khác, hỏng thì không được làm hỏng luồng chính).
    // Ở đây thất bại phải nổi lên thành mã lỗi HTTP, nếu không người dùng bấm xóa mà không có gì xảy ra.

    public async Task MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken ct = default)
    {
        var notification = await LoadOwnedAsync(notificationId, userId, ct);

        if (notification.IsRead) return;

        notification.MarkAsRead();
        await repository.UpdateAsync(notification, ct);
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

    public async Task DeleteAsync(Guid notificationId, Guid userId, CancellationToken ct = default)
    {
        await LoadOwnedAsync(notificationId, userId, ct);
        await repository.DeleteAsync(notificationId, ct);
    }

    /// <summary>
    /// Nạp thông báo và khẳng định nó thuộc về <paramref name="userId"/>. Thông báo của người khác
    /// trả 404 chứ không phải 403: 403 xác nhận "id này có tồn tại", đủ để dò xem người khác có
    /// thông báo nào — với người gọi thì tài nguyên không phải của họ là không tồn tại.
    /// </summary>
    private async Task<Notification> LoadOwnedAsync(Guid notificationId, Guid userId, CancellationToken ct)
    {
        var notification = await repository.GetByIdAsync(notificationId, ct);

        if (notification is null || notification.UserId != userId)
            throw new NotFoundException("Không tìm thấy thông báo.");

        return notification;
    }
}
