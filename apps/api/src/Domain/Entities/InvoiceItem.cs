namespace DentalClinic.API.Domain.Entities;

/// <summary>
/// Một dòng dịch vụ/thủ thuật trên hóa đơn.
/// Được "chụp" (snapshot) lại tại thời điểm xuất hóa đơn từ liệu trình điều trị.
/// </summary>
public class InvoiceItem
{
    public Guid Id { get; private set; }
    public Guid InvoiceId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }

    public Guid? TreatmentPlanId { get; private set; }
    public Guid? TreatmentPlanItemId { get; private set; }

    // Số tiền THU NGAY trên dòng này
    public decimal AmountCollected { get; private set; }

    // Thành tiền của dòng
    public decimal LineTotal => Quantity * UnitPrice;

    // Số còn nợ của dòng
    public decimal LineRemaining => Math.Max(0, LineTotal - AmountCollected);

    // Navigation properties
    public Invoice Invoice { get; private set; } = null!;
    public TreatmentPlanItem? TreatmentPlanItem { get; private set; }

    private InvoiceItem() { }

    public static InvoiceItem Create(
        Guid invoiceId,
        string name,
        int quantity,
        decimal unitPrice,
        Guid? treatmentPlanId = null,
        decimal? amountCollected = null,
        Guid? treatmentPlanItemId = null)
    {
        var qty = quantity < 1 ? 1 : quantity;
        var price = unitPrice < 0 ? 0 : unitPrice;
        var lineTotal = qty * price;
        return new InvoiceItem
        {
            Id = Guid.NewGuid(),
            InvoiceId = invoiceId,
            Name = name,
            Quantity = qty,
            UnitPrice = price,
            TreatmentPlanId = treatmentPlanId,
            TreatmentPlanItemId = treatmentPlanItemId,
            AmountCollected = amountCollected is decimal ac ? Math.Clamp(ac, 0, lineTotal) : lineTotal
        };
    }
}
