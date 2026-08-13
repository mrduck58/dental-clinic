using DentalClinic.API.Domain.Enums;

namespace DentalClinic.API.Domain.Entities;

/// <summary>
/// Hóa đơn thanh toán của một lịch hẹn.
/// Được staff xuất ra từ liệu trình điều trị sau khi bác sĩ kết thúc điều trị
/// (lịch hẹn chuyển sang trạng thái <see cref="AppointmentStatus.PendingPayment"/>).
/// </summary>
public class Invoice
{
    public Guid Id { get; private set; }
    public string InvoiceNumber { get; private set; } = string.Empty;
    public Guid AppointmentId { get; private set; }
    public decimal Subtotal { get; private set; }
    public decimal Discount { get; private set; }
    public decimal TotalAmount { get; private set; }       // Tổng chi phí điều trị (sau giảm giá)
    public PaymentType PaymentType { get; private set; }   // Toàn bộ hay đặt cọc
    public decimal DepositAmount { get; private set; }     // Số tiền thu trên hóa đơn này (= TotalAmount nếu thanh toán toàn bộ)
    public PaymentStatus Status { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? PaymentDate { get; private set; }

    // Công nợ / thu phần còn lại
    public Guid? ParentInvoiceId { get; private set; }   // hóa đơn gốc (đặt cọc) nếu đây là hóa đơn thu phần còn lại
    public bool IsSettled { get; private set; }          // hóa đơn đặt cọc đã được thu nốt phần còn lại
    public bool CollectingRemaining { get; private set; } // đang trong quy trình thu phần còn lại

    // Liệu trình điều trị — nếu hóa đơn này là một đợt thu của liệu trình
    public Guid? TreatmentPlanId { get; private set; }

    // Promotion áp dụng cho hóa đơn
    public Guid? PromotionId { get; private set; }
    public Promotion? Promotion { get; private set; }

    // Navigation properties
    public Appointment Appointment { get; private set; } = null!;
    public ICollection<InvoiceItem> Items { get; private set; } = new List<InvoiceItem>();
    public ICollection<PaymentTransaction> PaymentTransactions { get; private set; } = new List<PaymentTransaction>();

    private Invoice() { }

    /// <summary>
    /// Xuất hóa đơn từ danh sách dòng dịch vụ (lấy từ liệu trình điều trị).
    /// Tự tính tạm tính và tổng tiền sau khi trừ giảm giá.
    /// </summary>
    public static Invoice Issue(
        Guid appointmentId,
        string invoiceNumber,
        IEnumerable<(string Name, int Quantity, decimal UnitPrice, Guid? TreatmentPlanId, decimal? AmountCollected)> items,
        decimal discount,
        PaymentMethod paymentMethod,
        string? notes = null)
    {
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = invoiceNumber,
            AppointmentId = appointmentId,
            Status = PaymentStatus.Unpaid,
            PaymentMethod = paymentMethod,
            Notes = notes,
            CreatedAt = DateTimeOffset.UtcNow
        };

        foreach (var item in items)
            invoice.Items.Add(InvoiceItem.Create(invoice.Id, item.Name, item.Quantity, item.UnitPrice, item.TreatmentPlanId, item.AmountCollected));

        invoice.Subtotal = invoice.Items.Sum(i => i.LineTotal);
        invoice.Discount = discount < 0 ? 0 : discount;
        invoice.TotalAmount = Math.Max(0, invoice.Subtotal - invoice.Discount);

        // Số tiền thu ngay = tổng thu của từng dòng (thanh toán toàn bộ hoặc đặt cọc theo từng dịch vụ).
        var collected = invoice.Items.Sum(i => i.AmountCollected);
        invoice.DepositAmount = Math.Clamp(collected, 0, invoice.TotalAmount);
        invoice.PaymentType = invoice.DepositAmount < invoice.TotalAmount ? PaymentType.Deposit : PaymentType.Full;

        return invoice;
    }

    /// <summary>
    /// Tạo hóa đơn thu phần còn lại cho một hóa đơn đặt cọc.
    /// Đây là hóa đơn thanh toán toàn bộ số còn lại (1 dòng).
    /// </summary>
    public static Invoice IssueRemaining(
        Guid appointmentId,
        string invoiceNumber,
        Guid parentInvoiceId,
        string lineName,
        decimal remainingAmount,
        PaymentMethod paymentMethod,
        string? notes = null)
    {
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = invoiceNumber,
            AppointmentId = appointmentId,
            ParentInvoiceId = parentInvoiceId,
            Status = PaymentStatus.Unpaid,
            PaymentMethod = paymentMethod,
            PaymentType = PaymentType.Full,
            Notes = notes,
            CreatedAt = DateTimeOffset.UtcNow
        };
        invoice.Items.Add(InvoiceItem.Create(invoice.Id, lineName, 1, remainingAmount));
        invoice.Subtotal = remainingAmount;
        invoice.Discount = 0;
        invoice.TotalAmount = remainingAmount;
        invoice.DepositAmount = remainingAmount; // thu toàn bộ phần còn lại
        return invoice;
    }

    /// <summary>
    /// Tạo một đợt thu của liệu trình điều trị (cọc / đợt giữa / tất toán).
    /// Hóa đơn loại này chỉ là phiếu thu cho số tiền của đợt đó; công nợ được theo dõi ở cấp liệu trình.
    /// </summary>
    public static Invoice IssuePlanInstallment(
        Guid appointmentId,
        Guid treatmentPlanId,
        string invoiceNumber,
        string lineName,
        decimal amount,
        PaymentMethod paymentMethod,
        string? notes = null)
    {
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = invoiceNumber,
            AppointmentId = appointmentId,
            TreatmentPlanId = treatmentPlanId,
            Status = PaymentStatus.Unpaid,
            PaymentMethod = paymentMethod,
            PaymentType = PaymentType.Full,
            Notes = notes,
            CreatedAt = DateTimeOffset.UtcNow
        };
        invoice.Items.Add(InvoiceItem.Create(invoice.Id, lineName, 1, amount));
        invoice.Subtotal = amount;
        invoice.Discount = 0;
        invoice.TotalAmount = amount;
        invoice.DepositAmount = amount;
        return invoice;
    }

    /// <summary>Số tiền còn lại phải thu sau hóa đơn này.</summary>
    public decimal RemainingAmount => Math.Max(0, TotalAmount - DepositAmount);

    public void MarkAsPaid(PaymentMethod paymentMethod)
    {
        PaymentMethod = paymentMethod;
        Status = PaymentStatus.Paid;
        PaymentDate = DateTimeOffset.UtcNow;
    }

    /// <summary>Bắt đầu quy trình thu phần còn lại (đưa vào danh sách chờ xuất hóa đơn).</summary>
    public void StartCollectingRemaining() => CollectingRemaining = true;

    /// <summary>Đánh dấu đã tất toán (đã thu nốt phần còn lại).</summary>
    public void Settle()
    {
        IsSettled = true;
        CollectingRemaining = false;
    }

    public void Refund() => Status = PaymentStatus.Refunded;
}
