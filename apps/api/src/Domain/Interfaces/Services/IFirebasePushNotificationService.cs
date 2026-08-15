namespace DentalClinic.API.Domain.Interfaces.Services;

public interface IFirebasePushNotificationService
{
    Task RegisterTokenAsync(Guid userId, string token, string? deviceType, CancellationToken ct = default);
    Task SendPushNotificationAsync(Guid userId, string title, string body, string type, string? relatedEntityId, CancellationToken ct = default);
    Task SendPushNotificationToMultipleAsync(IEnumerable<Guid> userIds, string title, string body, string type, string? relatedEntityId, CancellationToken ct = default);
}
