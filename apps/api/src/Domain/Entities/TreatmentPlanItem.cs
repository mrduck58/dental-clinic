namespace DentalClinic.API.Domain.Entities;

using DentalClinic.API.Domain.Enums;

/// <summary>
/// Một mục dịch vụ được chỉ định trong Kế hoạch điều trị của bệnh nhân.
/// </summary>
public class TreatmentPlanItem
{
    public Guid Id { get; private set; }
    public Guid TreatmentPlanId { get; private set; }
    public Guid ServiceId { get; private set; }
    public Guid? ServiceOptionId { get; private set; }
    public string? ServiceOptionName { get; private set; }
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public string? Teeth { get; private set; }
    public int? EstimatedSessionCount { get; private set; }
    public int? EstimatedDurationMin { get; private set; }
    public int? EstimatedDurationMax { get; private set; }
    public DurationUnit? EstimatedDurationUnit { get; private set; }
    public DateOnly? EstimatedStartDate { get; private set; }
    public DateOnly? EstimatedEndDate { get; private set; }
    public TreatmentPlanItemStatus Status { get; private set; }
    public DateOnly? WarrantyUntil { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    public decimal TotalCost => UnitPrice * Quantity;

    // Navigation properties
    public TreatmentPlan TreatmentPlan { get; private set; } = null!;
    public Service Service { get; set; } = null!;
    public ServiceOption? ServiceOption { get; private set; }
    public ICollection<TreatmentSession> Sessions { get; private set; } = new List<TreatmentSession>();
    public ICollection<InvoiceItem> InvoiceItems { get; private set; } = new List<InvoiceItem>();
    public ICollection<FollowUp> FollowUps { get; private set; } = new List<FollowUp>();

    private TreatmentPlanItem() { }

    public static TreatmentPlanItem Create(
        Guid treatmentPlanId,
        Guid serviceId,
        decimal unitPrice,
        int quantity,
        string? teeth = null,
        string? notes = null,
        DateOnly? warrantyUntil = null,
        Guid? serviceOptionId = null,
        string? serviceOptionName = null,
        int? estimatedSessionCount = null,
        int? estimatedDurationMin = null,
        int? estimatedDurationMax = null,
        DurationUnit? estimatedDurationUnit = null,
        DateOnly? estimatedStartDate = null,
        DateOnly? estimatedEndDate = null)
    {
        return new TreatmentPlanItem
        {
            Id = Guid.NewGuid(),
            TreatmentPlanId = treatmentPlanId,
            ServiceId = serviceId,
            ServiceOptionId = serviceOptionId,
            ServiceOptionName = serviceOptionName,
            UnitPrice = unitPrice < 0 ? 0 : unitPrice,
            Quantity = quantity < 1 ? 1 : quantity,
            Teeth = teeth,
            Notes = notes,
            WarrantyUntil = warrantyUntil,
            EstimatedSessionCount = estimatedSessionCount,
            EstimatedDurationMin = estimatedDurationMin,
            EstimatedDurationMax = estimatedDurationMax,
            EstimatedDurationUnit = estimatedDurationUnit,
            EstimatedStartDate = estimatedStartDate,
            EstimatedEndDate = estimatedEndDate,
            Status = TreatmentPlanItemStatus.Planned,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Update(
        decimal unitPrice,
        int quantity,
        string? teeth,
        string? notes,
        DateOnly? warrantyUntil,
        int? estimatedSessionCount = null,
        int? estimatedDurationMin = null,
        int? estimatedDurationMax = null,
        DurationUnit? estimatedDurationUnit = null,
        DateOnly? estimatedStartDate = null,
        DateOnly? estimatedEndDate = null)
    {
        UnitPrice = unitPrice < 0 ? 0 : unitPrice;
        Quantity = quantity < 1 ? 1 : quantity;
        Teeth = teeth;
        Notes = notes;
        WarrantyUntil = warrantyUntil;
        EstimatedSessionCount = estimatedSessionCount;
        EstimatedDurationMin = estimatedDurationMin;
        EstimatedDurationMax = estimatedDurationMax;
        EstimatedDurationUnit = estimatedDurationUnit;
        EstimatedStartDate = estimatedStartDate;
        EstimatedEndDate = estimatedEndDate;
    }

    public void SetStatus(TreatmentPlanItemStatus status)
    {
        Status = status;
        CompletedAt = status == TreatmentPlanItemStatus.Completed ? DateTimeOffset.UtcNow : null;
    }
}
