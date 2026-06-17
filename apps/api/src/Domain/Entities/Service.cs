namespace DentalClinic.API.Domain.Entities;

public class Service
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public int DurationMinutes { get; private set; }
    public bool IsActive { get; private set; } = true;
    public string Description { get; private set; } = string.Empty;
    public int ViewCount { get; private set; }
    public string? ImageUrl { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    private Service() { }

    public static Service Create(
        string name,
        decimal price,
        int durationMinutes,
        string description,
        string? imageUrl = null)
        => new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Price = price,
            DurationMinutes = durationMinutes,
            IsActive = true,
            Description = description,
            ViewCount = 0,
            ImageUrl = imageUrl,
            CreatedAt = DateTimeOffset.UtcNow,
        };

    public void Update(
        string name,
        decimal price,
        int durationMinutes,
        string description,
        string? imageUrl)
    {
        Name = name;
        Price = price;
        DurationMinutes = durationMinutes;
        Description = description;
        if (imageUrl is not null) ImageUrl = imageUrl;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
