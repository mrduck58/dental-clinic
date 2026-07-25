namespace DentalClinic.API.Domain.Entities;

public class SupplyTransaction
{
    public Guid Id { get; private set; }
    public Guid SupplyItemId { get; private set; }
    public SupplyItem SupplyItem { get; private set; } = null!;
    public string Type { get; private set; } = string.Empty; // "import" | "export"
    public int Quantity { get; private set; }
    public string? Note { get; private set; }
    public string CreatedBy { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }

    private SupplyTransaction() { }

    public static SupplyTransaction Create(Guid supplyItemId, string type, int quantity, string? note, string createdBy)
        => new()
        {
            Id = Guid.NewGuid(),
            SupplyItemId = supplyItemId,
            Type = type,
            Quantity = quantity,
            Note = note,
            CreatedBy = createdBy,
            CreatedAt = DateTimeOffset.UtcNow,
        };
}
