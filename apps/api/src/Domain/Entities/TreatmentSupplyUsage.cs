namespace DentalClinic.API.Domain.Entities;

/// <summary>
/// Vật tư ĐÃ THỰC SỰ dùng cho một liệu trình điều trị — ghi nhận lúc bác sĩ xác nhận trong buổi khám
/// (không phải lúc lập liệu trình, vì liệu trình nhiều buổi thì vật tư tiêu hao dần qua từng buổi).
/// Mỗi dòng luôn kèm 1 SupplyTransaction loại "export" phát sinh cùng lúc để trừ kho thật —
/// UnitCostAtUsage chụp lại giá vốn tại thời điểm dùng (SupplyItem.Price có thể đổi về sau).
/// </summary>
public class TreatmentSupplyUsage
{
    public Guid Id { get; private set; }
    public Guid TreatmentPlanId { get; private set; }
    public Guid SupplyItemId { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitCostAtUsage { get; private set; }
    public Guid SupplyTransactionId { get; private set; }
    public string CreatedBy { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    /// <summary>Id của mục trong StepProgressJson mà lần ghi nhận này gắn theo (null = ghi nhận không rõ
    /// gắn với bước nào cụ thể, chỉ xảy ra với dữ liệu cũ trước khi có liên kết này). Dùng để hoàn kho đúng
    /// dòng khi bước đó bị xóa khỏi nhật ký điều trị — xem DeleteStepProgressHandler.</summary>
    public Guid? StepEntryId { get; private set; }
    /// <summary>True nếu đã được hoàn kho lại (do bước điều trị gắn với lần dùng này bị xóa) — vẫn giữ bản
    /// ghi để lưu vết, không xóa, để biết vì sao kho tăng trở lại.</summary>
    public bool IsReversed { get; private set; }

    // Navigation
    public TreatmentPlan TreatmentPlan { get; private set; } = null!;
    public SupplyItem SupplyItem { get; private set; } = null!;
    public SupplyTransaction SupplyTransaction { get; private set; } = null!;

    private TreatmentSupplyUsage() { }

    public static TreatmentSupplyUsage Create(
        Guid treatmentPlanId, Guid supplyItemId, int quantity, decimal unitCostAtUsage,
        Guid supplyTransactionId, string createdBy, Guid? stepEntryId = null)
        => new()
        {
            Id = Guid.NewGuid(),
            TreatmentPlanId = treatmentPlanId,
            SupplyItemId = supplyItemId,
            Quantity = quantity,
            UnitCostAtUsage = unitCostAtUsage,
            SupplyTransactionId = supplyTransactionId,
            CreatedBy = createdBy,
            CreatedAt = DateTimeOffset.UtcNow,
            StepEntryId = stepEntryId,
        };

    public void MarkReversed() => IsReversed = true;
}
