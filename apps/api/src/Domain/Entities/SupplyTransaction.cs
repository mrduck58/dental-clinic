namespace DentalClinic.API.Domain.Entities;

public class SupplyTransaction
{
    public Guid Id { get; private set; }
    public Guid SupplyItemId { get; private set; }
    public SupplyItem SupplyItem { get; private set; } = null!;
    public string Type { get; private set; } = string.Empty; // "import" | "export"
    public int Quantity { get; private set; }
    // Đơn giá của LẦN nhập này (mỗi lần nhập có thể mua với giá khác nhau) — chỉ có ý nghĩa khi Type="import".
    public decimal? UnitPrice { get; private set; }
    public string? Note { get; private set; }
    public string CreatedBy { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }

    public Guid? EmployeeId { get; private set; }
    public Employee? Employee { get; private set; }

    // Phòng nhận hàng khi Type="export" theo yêu cầu miệng của bác sĩ (vd hết găng tay giữa ca khám) —
    // null nếu không phải xuất theo phòng (import, hoặc export khác như tiêu hao điều trị/theo yêu cầu vật tư).
    public Guid? RoomId { get; private set; }
    public Room? Room { get; private set; }

    private SupplyTransaction() { }

    public static SupplyTransaction Create(
        Guid supplyItemId, string type, int quantity, string? note, string createdBy, decimal? unitPrice = null,
        Guid? roomId = null)
        => new()
        {
            Id = Guid.NewGuid(),
            SupplyItemId = supplyItemId,
            Type = type,
            Quantity = quantity,
            UnitPrice = unitPrice,
            Note = note,
            CreatedBy = createdBy,
            CreatedAt = DateTimeOffset.UtcNow,
            RoomId = roomId,
        };
}
