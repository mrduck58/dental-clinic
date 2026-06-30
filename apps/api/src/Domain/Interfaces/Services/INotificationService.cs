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

    Task MarkAsReadAsync(Guid notificationId, CancellationToken ct = default);
    Task MarkAllAsReadAsync(Guid userId, CancellationToken ct = default);
    Task DeleteAsync(Guid notificationId, CancellationToken ct = default);
}
