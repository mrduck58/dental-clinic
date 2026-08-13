namespace DentalClinic.API.Domain.Entities;

/// <summary>
/// Một dòng vật tư trong yêu cầu của bác sĩ (tên, số lượng, đơn vị).
/// Khi staff bấm "Đã xử lý" trên MaterialRequest, mỗi item được nhập thẳng vào kho —
/// SupplyTransactionId lưu lại giao dịch kho phát sinh từ đó để truy vết.
/// </summary>
public class MaterialRequestItem
{
    public Guid Id { get; private set; }
    public Guid MaterialRequestId { get; private set; }
    public string ItemName { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public string Unit { get; private set; } = string.Empty;
    public Guid? SupplyTransactionId { get; private set; }

    public Guid? SupplyItemId { get; private set; }
    public SupplyItem? SupplyItem { get; private set; }

    private MaterialRequestItem() { }

    public static MaterialRequestItem Create(Guid materialRequestId, string itemName, int quantity, string unit)
        => new()
        {
            Id = Guid.NewGuid(),
            MaterialRequestId = materialRequestId,
            ItemName = itemName,
            Quantity = quantity,
            Unit = unit,
        };

    public void LinkSupplyTransaction(Guid supplyTransactionId)
        => SupplyTransactionId = supplyTransactionId;
}
