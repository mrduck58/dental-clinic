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
    // "standard" = vật dụng thường ngày (tồn kho dùng dần); "custom" = hàng đặt riêng cho bệnh nhân (răng sứ,
    // hàm tháo lắp...) — vẫn dùng chung cơ chế tồn kho/nhập-xuất, chỉ khác ở việc phân tab hiển thị.
    public string OrderType { get; private set; } = "standard";
    // Giá tham chiếu hiện tại của vật tư (hiển thị ở bảng tồn kho) — khác với SupplyTransaction.UnitPrice là giá
    // thực trả ở TỪNG lần nhập cụ thể. Tự động cập nhật theo giá của lần nhập gần nhất (xem StockImportHandler).
    public decimal? Price { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    public ICollection<SupplyTransaction> Transactions { get; private set; } = [];

    private SupplyItem() { }

    public static SupplyItem Create(
        string code, string name, string category, string unit, int quantity, int minQuantity,
        string orderType = "standard", decimal? price = null)
        => new()
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            Category = category,
            Unit = unit,
            Quantity = quantity,
            MinQuantity = minQuantity,
            OrderType = orderType,
            Price = price,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };

    public void AdjustQuantity(int delta)
    {
        Quantity = Math.Max(0, Quantity + delta);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdatePrice(decimal price)
    {
        Price = price;
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
