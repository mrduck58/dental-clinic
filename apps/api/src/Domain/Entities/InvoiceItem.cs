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

    // Thành tiền của dòng — tính toán, không lưu xuống DB.
    public decimal LineTotal => Quantity * UnitPrice;

    // Navigation property
    public Invoice Invoice { get; private set; } = null!;

    private InvoiceItem() { }

    public static InvoiceItem Create(Guid invoiceId, string name, int quantity, decimal unitPrice)
    {
        return new InvoiceItem
        {
            Id = Guid.NewGuid(),
            InvoiceId = invoiceId,
            Name = name,
            Quantity = quantity < 1 ? 1 : quantity,
            UnitPrice = unitPrice < 0 ? 0 : unitPrice
        };
    }
}
