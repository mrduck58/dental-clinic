using DentalClinic.API.Domain.Enums;

namespace DentalClinic.API.Domain.Entities;

public class Service
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public int DurationMinutes { get; private set; }
    public int? EstimatedSessionCount { get; private set; }
    public int? EstimatedDurationMin { get; private set; }
    public int? EstimatedDurationMax { get; private set; }
    public DurationUnit? EstimatedDurationUnit { get; private set; }
    public bool IsActive { get; private set; } = true;
    public string Description { get; private set; } = string.Empty;
    /// <summary>Nội dung bài viết HTML mô tả chi tiết dịch vụ (hiển thị trên clinic_website).</summary>
    public string Content { get; private set; } = string.Empty;
    public int ViewCount { get; private set; }
    public string? ImageUrl { get; private set; }
    public string? IconUrl { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    // Navigation
    private readonly List<ServiceOption> _options = [];
    public IReadOnlyCollection<ServiceOption> Options => _options.AsReadOnly();

    private Service() { }

    public static Service Create(
        string name,
        decimal price,
        int durationMinutes,
        string description,
        string content = "",
        string? imageUrl = null,
        string? iconUrl = null,
        int? estimatedSessionCount = null,
        int? estimatedDurationMin = null,
        int? estimatedDurationMax = null,
        DurationUnit? estimatedDurationUnit = null)
        => new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Price = price,
            DurationMinutes = durationMinutes,
            EstimatedSessionCount = estimatedSessionCount,
            EstimatedDurationMin = estimatedDurationMin,
            EstimatedDurationMax = estimatedDurationMax,
            EstimatedDurationUnit = estimatedDurationUnit,
            IsActive = true,
            Description = description,
            Content = content,
            ViewCount = 0,
            ImageUrl = imageUrl,
            IconUrl = iconUrl,
            CreatedAt = DateTimeOffset.UtcNow,
        };

    public void Update(
        string name,
        decimal price,
        int durationMinutes,
        string description,
        string content,
        string? imageUrl,
        string? iconUrl,
        int? estimatedSessionCount = null,
        int? estimatedDurationMin = null,
        int? estimatedDurationMax = null,
        DurationUnit? estimatedDurationUnit = null)
    {
        Name = name;
        Price = price;
        DurationMinutes = durationMinutes;
        Description = description;
        Content = content;
        if (imageUrl is not null) ImageUrl = imageUrl;
        if (iconUrl is not null) IconUrl = iconUrl;
        EstimatedSessionCount = estimatedSessionCount;
        EstimatedDurationMin = estimatedDurationMin;
        EstimatedDurationMax = estimatedDurationMax;
        EstimatedDurationUnit = estimatedDurationUnit;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void AddOption(string name, decimal price, string unit, int sortOrder)
    {
        _options.Add(ServiceOption.Create(Id, name, price, unit, sortOrder));
    }

    public void ReplaceOptions(IEnumerable<(string Name, decimal Price, string Unit, int SortOrder)> newOptions)
    {
        _options.Clear();
        var sorted = newOptions.OrderBy(o => o.SortOrder).ToList();
        for (int i = 0; i < sorted.Count; i++)
        {
            _options.Add(ServiceOption.Create(Id, sorted[i].Name, sorted[i].Price, sorted[i].Unit, i));
        }
    }
}
