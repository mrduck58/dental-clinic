namespace DentalClinic.API.Application.DTOs.Notifications;

public record NotificationDto(
    Guid Id,
    string Type,
    string Priority,
    string Title,
    string Body,
    bool IsRead,
    DateTimeOffset? ReadAt,
    string? RelatedEntityType,
    string? RelatedEntityId,
    DateTimeOffset CreatedAt);

public record NotificationPagedDto(
    IReadOnlyList<NotificationDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages,
    int UnreadCount);
