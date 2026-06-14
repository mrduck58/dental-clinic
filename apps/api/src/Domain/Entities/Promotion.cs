using System.Text.Json;

namespace DentalClinic.API.Domain.Entities;

public class Promotion
{
    public Guid Id { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string DiscountType { get; private set; } = "Percentage";
    public decimal DiscountValue { get; private set; }
    public string ServiceIdsJson { get; private set; } = "[]";
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    public List<Guid> GetServiceIds() =>
        JsonSerializer.Deserialize<List<Guid>>(ServiceIdsJson) ?? [];

    private Promotion() { }

    public static Promotion Create(
        string code, string name, string? description,
        string discountType, decimal discountValue,
        List<Guid> serviceIds,
        DateOnly startDate, DateOnly endDate,
        bool isActive) => new()
    {
        Id = Guid.NewGuid(),
        Code = code.Trim().ToUpper(),
        Name = name,
        Description = description,
        DiscountType = discountType,
        DiscountValue = discountValue,
        ServiceIdsJson = JsonSerializer.Serialize(serviceIds),
        StartDate = startDate,
        EndDate = endDate,
        IsActive = isActive,
        CreatedAt = DateTimeOffset.UtcNow,
    };

    public void Update(
        string code, string name, string? description,
        string discountType, decimal discountValue,
        List<Guid> serviceIds,
        DateOnly startDate, DateOnly endDate)
    {
        Code = code.Trim().ToUpper();
        Name = name;
        Description = description;
        DiscountType = discountType;
        DiscountValue = discountValue;
        ServiceIdsJson = JsonSerializer.Serialize(serviceIds);
        StartDate = startDate;
        EndDate = endDate;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
