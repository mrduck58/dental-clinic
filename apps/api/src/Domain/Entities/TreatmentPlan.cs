using DentalClinic.API.Domain.Enums;

namespace DentalClinic.API.Domain.Entities;

/// <summary>
/// Một mục liệu trình điều trị: mỗi dòng là một dịch vụ chỉ định cho bệnh nhân
/// (giá, số lượng, răng điều trị, bảo hành). Hóa đơn thu từng đợt tham chiếu
/// qua Invoice.TreatmentPlanId; công nợ = TotalCost - tổng các hóa đơn đã thanh toán.
/// </summary>
public class TreatmentPlan
{
    public Guid Id { get; private set; }
    public Guid PatientId { get; private set; }
    public Guid DentistId { get; private set; }
    public Guid? AppointmentId { get; private set; } // Buổi hẹn lập liệu trình (SetNull khi xóa hẹn)
    public Guid ServiceId { get; private set; }
    // Tên option đã chọn lúc thêm dịch vụ (vd: "Titan", "Zirconia") — SNAPSHOT theo tên, KHÔNG phải FK tới
    // ServiceOption.Id. ServiceOption bị xoá sạch và tạo lại toàn bộ mỗi khi sửa dịch vụ (Service.ReplaceOptions),
    // nên một FK thật sẽ vỡ (hoặc bị SetNull mất dấu vết) ngay lần đầu admin sửa dịch vụ. Null = dùng giá gốc
    // dịch vụ, không chọn option nào.
    public string? ServiceOptionName { get; private set; }
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public string? Teeth { get; private set; }              // Răng điều trị, ví dụ "21, 36"
    public TreatmentPlanStatus Status { get; private set; }
    public DateOnly? WarrantyUntil { get; private set; }    // Bảo hành đến
    public string? StepProgressJson { get; private set; }   // Nhật ký các bước đã thực hiện (JSON array)
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    public decimal TotalCost => UnitPrice * Quantity;

    // Navigation properties
    public Patient Patient { get; private set; } = null!;
    public DentistProfile Dentist { get; private set; } = null!;
    public Appointment? Appointment { get; private set; }
    public Service Service { get; private set; } = null!;
    public ICollection<Invoice> Invoices { get; private set; } = new List<Invoice>();

    private TreatmentPlan() { }

    public static TreatmentPlan Create(
        Guid patientId,
        Guid dentistId,
        Guid? appointmentId,
        Guid serviceId,
        decimal unitPrice,
        int quantity,
        string? teeth = null,
        string? notes = null,
        DateOnly? warrantyUntil = null,
        string? serviceOptionName = null)
    {
        return new TreatmentPlan
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            DentistId = dentistId,
            AppointmentId = appointmentId,
            ServiceId = serviceId,
            ServiceOptionName = serviceOptionName,
            UnitPrice = unitPrice < 0 ? 0 : unitPrice,
            Quantity = quantity < 1 ? 1 : quantity,
            Teeth = teeth,
            Notes = notes,
            WarrantyUntil = warrantyUntil,
            Status = TreatmentPlanStatus.Planned,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Update(decimal unitPrice, int quantity, string? teeth, string? notes, DateOnly? warrantyUntil)
    {
        UnitPrice = unitPrice < 0 ? 0 : unitPrice;
        Quantity = quantity < 1 ? 1 : quantity;
        Teeth = teeth;
        Notes = notes;
        WarrantyUntil = warrantyUntil;
    }

    public void SetStatus(TreatmentPlanStatus status)
    {
        Status = status;
        CompletedAt = status == TreatmentPlanStatus.Completed ? DateTimeOffset.UtcNow : null;
    }

    public void UpdateStepProgress(string? json) => StepProgressJson = json;
}
