namespace DentalClinic.API.Domain.Interfaces.Services;

public record CreateNotificationRequest(
    Guid UserId,
    string Type,
    string Priority,
    string Title,
    string Body,
    string? RelatedEntityType = null,
    string? RelatedEntityId = null);

public interface INotificationService
{
    Task CreateAsync(CreateNotificationRequest request, CancellationToken ct = default);

    Task CreateForMultipleUsersAsync(
        IEnumerable<Guid> userIds,
        CreateNotificationRequest template,
        CancellationToken ct = default);

    /// <summary>Đánh dấu đã đọc. <paramref name="userId"/> là chủ sở hữu bắt buộc — thông báo của
    /// người khác bị coi như không tồn tại (ném <c>NotFoundException</c>).</summary>
    Task MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken ct = default);

    Task MarkAllAsReadAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Xóa thông báo. <paramref name="userId"/> là chủ sở hữu bắt buộc — thông báo của
    /// người khác bị coi như không tồn tại (ném <c>NotFoundException</c>).</summary>
    Task DeleteAsync(Guid notificationId, Guid userId, CancellationToken ct = default);

    Task DeleteAllAsync(Guid userId, CancellationToken ct = default);
}
