namespace DentalClinic.API.Domain.Entities;

/// <summary>
/// Vật tư ĐÃ THỰC SỰ dùng cho một liệu trình/phiên điều trị — ghi nhận lúc bác sĩ xác nhận trong buổi khám.
/// </summary>
public class TreatmentSupplyUsage
{
    public Guid Id { get; private set; }
    public Guid? TreatmentPlanId { get; private set; }
    public Guid? TreatmentSessionId { get; private set; }
    public Guid SupplyItemId { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitCostAtUsage { get; private set; }
    public Guid SupplyTransactionId { get; private set; }
    public string CreatedBy { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid? StepEntryId { get; private set; }
    public bool IsReversed { get; private set; }

    // Navigation
    public TreatmentPlan? TreatmentPlan { get; private set; }
    public TreatmentSession? TreatmentSession { get; private set; }
    public SupplyItem SupplyItem { get; private set; } = null!;
    public SupplyTransaction SupplyTransaction { get; private set; } = null!;

    private TreatmentSupplyUsage() { }

    public static TreatmentSupplyUsage Create(
        Guid? treatmentPlanId,
        Guid supplyItemId,
        int quantity,
        decimal unitCostAtUsage,
        Guid supplyTransactionId,
        string createdBy,
        Guid? stepEntryId = null,
        Guid? treatmentSessionId = null)
        => new()
        {
            Id = Guid.NewGuid(),
            TreatmentPlanId = treatmentPlanId,
            TreatmentSessionId = treatmentSessionId,
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
