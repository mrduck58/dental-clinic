namespace DentalClinic.API.Application.DTOs.Posts;

public record PostDto(
    Guid Id,
    string Title,
    string Category,
    string Author,
    string Content,
    string? ThumbnailUrl,
    bool IsPublished,
    Guid? ServiceId,
    string? ServiceName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    DateTimeOffset? PublishedAt);

public record CreatePostRequest(
    string Title,
    string Category,
    string Author,
    string Content,
    string? ThumbnailUrl,
    bool IsPublished,
    Guid? ServiceId);

public record UpdatePostRequest(
    string Title,
    string Category,
    string Content,
    string? ThumbnailUrl,
    bool IsPublished,
    Guid? ServiceId);
