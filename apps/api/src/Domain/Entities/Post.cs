namespace DentalClinic.API.Domain.Entities;

public class Post
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public string Author { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public string? ThumbnailUrl { get; private set; }
    public bool IsPublished { get; private set; }

    /// <summary>Dịch vụ mà bài viết này mô tả (null = tin tức chung, không gắn dịch vụ nào).</summary>
    public Guid? ServiceId { get; private set; }
    public Service? Service { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }

    private Post() { }

    public static Post Create(
        string title,
        string category,
        string author,
        string content,
        string? thumbnailUrl,
        bool isPublished,
        Guid? serviceId = null)
        => new()
        {
            Id = Guid.NewGuid(),
            Title = title,
            Category = category,
            Author = author,
            Content = content,
            ThumbnailUrl = thumbnailUrl,
            IsPublished = isPublished,
            ServiceId = serviceId,
            CreatedAt = DateTimeOffset.UtcNow,
            PublishedAt = isPublished ? DateTimeOffset.UtcNow : null,
        };

    public void Update(
        string title,
        string category,
        string content,
        string? thumbnailUrl,
        bool isPublished,
        Guid? serviceId = null)
    {
        Title = title;
        Category = category;
        Content = content;
        if (thumbnailUrl is not null) ThumbnailUrl = thumbnailUrl;
        ServiceId = serviceId;
        if (isPublished && !IsPublished) PublishedAt = DateTimeOffset.UtcNow;
        IsPublished = isPublished;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
