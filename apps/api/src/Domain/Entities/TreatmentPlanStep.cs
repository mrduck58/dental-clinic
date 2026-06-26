using DentalClinic.API.Domain.Enums;

namespace DentalClinic.API.Domain.Entities;

public class TreatmentPlanStep
{
    public Guid Id { get; private set; }
    public Guid TreatmentPlanId { get; private set; }
    public int StepNumber { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public TreatmentStepStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    // Navigation property
    public TreatmentPlan TreatmentPlan { get; private set; } = null!;

    private TreatmentPlanStep() { }

    public static TreatmentPlanStep Create(
        Guid treatmentPlanId,
        int stepNumber,
        string description,
        string? notes = null)
    {
        return new TreatmentPlanStep
        {
            Id = Guid.NewGuid(),
            TreatmentPlanId = treatmentPlanId,
            StepNumber = stepNumber,
            Description = description,
            Status = TreatmentStepStatus.Pending,
            Notes = notes,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Update(string description, string? notes)
    {
        Description = description;
        Notes = notes;
    }

    public void Complete() => Status = TreatmentStepStatus.Completed;
    public void Reset() => Status = TreatmentStepStatus.Pending;
}
