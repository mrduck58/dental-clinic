namespace DentalClinic.API.Application.DTOs.Invoices;

/// <summary>Một dòng dịch vụ trên hóa đơn (hoặc gợi ý từ liệu trình). ServiceId để frontend tự khớp
/// khuyến mãi theo đúng dịch vụ (không phải so tên chuỗi) khi xem trước trước khi xuất hóa đơn thật.</summary>
public record InvoiceItemDto(string Name, int Quantity, decimal UnitPrice, Guid? TreatmentPlanId = null, Guid? ServiceId = null)
{
    public decimal LineTotal => Quantity * UnitPrice;
}

/// <summary>
/// Liệu trình điều trị đang chờ xuất hóa đơn — gom theo lịch hẹn đã kết thúc điều trị
/// (trạng thái PendingPayment) và chưa có hóa đơn.
/// </summary>
public class BillablePlanDto
{
    public Guid AppointmentId { get; set; }
    public string AppointmentCode { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string? PatientPhone { get; set; }
    public string? Gender { get; set; }
    public string DentistName { get; set; } = string.Empty;
    public DateTimeOffset AppointmentDate { get; set; }
    public string Diagnosis { get; set; } = string.Empty;
    public List<InvoiceItemDto> Items { get; set; } = new();
    public decimal SuggestedTotal { get; set; }

    // Khi mục này là "thu phần còn lại" của một hóa đơn đặt cọc
    public Guid? OutstandingInvoiceId { get; set; }
    public string? SourceInvoiceNumber { get; set; }

    // Khi mục này là một đợt thu của liệu trình điều trị
    public Guid? TreatmentPlanId { get; set; }
    public string? PlanName { get; set; }
    public decimal PlanTotal { get; set; }
    public decimal PlanAmountPaid { get; set; }
    public decimal PlanRemaining { get; set; }
}

/// <summary>Công nợ ở cấp liệu trình điều trị (cho tab Công nợ).</summary>
public class OutstandingPlanDto
{
    public Guid TreatmentPlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string? PatientPhone { get; set; }
    public string? Gender { get; set; }
    public string DentistName { get; set; } = string.Empty;
    public decimal TotalCost { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal RemainingAmount { get; set; }

    /// <summary>
    /// Phần chi phí CHƯA gắn vào hóa đơn nào — số tiền còn phải xuất hóa đơn ở các đợt thu sau.
    /// Phần đã xuất hóa đơn mà chưa thu không nằm ở đây: nó là công nợ của hóa đơn đó
    /// (tab "Hóa đơn đặt cọc còn nợ"), tính vào cả hai chỗ là cộng trùng một khoản nợ.
    /// </summary>
    public decimal UnbilledAmount { get; set; }

    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>Hóa đơn đầy đủ trả về cho tab "Chờ thanh toán" và "Lịch sử".</summary>
public class InvoiceDto
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public Guid AppointmentId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string? PatientPhone { get; set; }
    public string? Gender { get; set; }
    public string DentistName { get; set; } = string.Empty;
    public DateTimeOffset AppointmentDate { get; set; }
    public List<InvoiceItemDto> Items { get; set; } = new();
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public Guid? PromotionId { get; set; }
    public string? PromotionCode { get; set; }
    public string? PromotionName { get; set; }
    public decimal TotalAmount { get; set; }
    public string PaymentType { get; set; } = string.Empty;   // "Full" | "Deposit"
    public decimal DepositAmount { get; set; }                // Số tiền thu trên hóa đơn này
    public decimal RemainingAmount { get; set; }              // Số tiền còn lại
    public string PaymentMethod { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? PaymentDate { get; set; }

    // Công nợ
    public Guid? ParentInvoiceId { get; set; }     // hóa đơn gốc nếu đây là hóa đơn thu phần còn lại
    public bool IsSettled { get; set; }            // hóa đơn đặt cọc đã tất toán
    public bool CollectingRemaining { get; set; }  // đang trong quy trình thu phần còn lại
}

public record IssueInvoiceItemRequest(string Name, int Quantity, decimal UnitPrice, Guid? TreatmentPlanId = null, decimal? AmountCollected = null);

public record ConfirmPaymentRequest(string? PaymentMethod);
