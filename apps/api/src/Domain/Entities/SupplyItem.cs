namespace DentalClinic.API.Domain.Entities;

public class SupplyItem
{
    public Guid Id { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public string Unit { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public int MinQuantity { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    public ICollection<SupplyTransaction> Transactions { get; private set; } = [];

    private SupplyItem() { }

    public static SupplyItem Create(string code, string name, string category, string unit, int quantity, int minQuantity)
        => new()
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            Category = category,
            Unit = unit,
            Quantity = quantity,
            MinQuantity = minQuantity,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };

    public void AdjustQuantity(int delta)
    {
        Quantity = Math.Max(0, Quantity + delta);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Update(string name, string category, string unit, int minQuantity)
    {
        Name = name;
        Category = category;
        Unit = unit;
        MinQuantity = minQuantity;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
