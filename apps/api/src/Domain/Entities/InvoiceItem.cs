namespace DentalClinic.API.Domain.Entities;

/// <summary>
/// Một dòng dịch vụ/thủ thuật trên hóa đơn.
/// Được "chụp" (snapshot) lại tại thời điểm xuất hóa đơn từ liệu trình điều trị,
/// để hóa đơn không bị thay đổi khi liệu trình gốc được chỉnh sửa về sau.
/// </summary>
public class InvoiceItem
{
    public Guid Id { get; private set; }
    public Guid InvoiceId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }

    // Liệu trình điều trị mà dòng này thu tiền cho — để credit đúng công nợ từng liệu trình khi thanh toán.
    // Null với các dòng không gắn liệu trình (VD: thu phần còn lại của hóa đơn cọc).
    public Guid? TreatmentPlanId { get; private set; }

    // Số tiền THU NGAY trên dòng này (thanh toán toàn bộ = LineTotal; đặt cọc = số cọc của dòng).
    // Phần còn lại của dòng = LineTotal - AmountCollected sẽ vào công nợ.
    public decimal AmountCollected { get; private set; }

    // Thành tiền của dòng — tính toán, không lưu xuống DB.
    public decimal LineTotal => Quantity * UnitPrice;

    // Số còn nợ của dòng.
    public decimal LineRemaining => Math.Max(0, LineTotal - AmountCollected);

    // Navigation property
    public Invoice Invoice { get; private set; } = null!;

    private InvoiceItem() { }

    public static InvoiceItem Create(Guid invoiceId, string name, int quantity, decimal unitPrice,
        Guid? treatmentPlanId = null, decimal? amountCollected = null)
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
            // Mặc định thu toàn bộ dòng; nếu truyền vào thì kẹp trong [0, LineTotal].
            AmountCollected = amountCollected is decimal ac ? Math.Clamp(ac, 0, lineTotal) : lineTotal
        };
    }
}
