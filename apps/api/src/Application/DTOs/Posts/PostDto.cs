namespace DentalClinic.API.Application.DTOs.Posts;

public record PostDto(
    Guid Id,
    string Title,
    string Category,
    string Author,
    string Content,
    string? ThumbnailUrl,
    bool IsPublished,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    DateTimeOffset? PublishedAt);

public record CreatePostRequest(
    string Title,
    string Category,
    string Author,
    string Content,
    string? ThumbnailUrl,
    bool IsPublished);

public record UpdatePostRequest(
    string Title,
    string Category,
    string Content,
    string? ThumbnailUrl,
    bool IsPublished);
