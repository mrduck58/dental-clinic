using DentalClinic.API.Domain.Enums;

namespace DentalClinic.API.Domain.Entities;

public class TreatmentPlan
{
    public Guid Id { get; private set; }
    public Guid AppointmentId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public TreatmentPlanStatus Status { get; private set; }
    public decimal? EstimatedCost { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    // Navigation property
    public Appointment Appointment { get; private set; } = null!;

    private TreatmentPlan() { }

    public static TreatmentPlan Create(
        Guid appointmentId,
        string description,
        decimal? estimatedCost = null)
    {
        return new TreatmentPlan
        {
            Id = Guid.NewGuid(),
            AppointmentId = appointmentId,
            Description = description,
            Status = TreatmentPlanStatus.Planned,
            EstimatedCost = estimatedCost,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Update(string description, decimal? estimatedCost)
    {
        Description = description;
        EstimatedCost = estimatedCost;
    }

    public void Start() => Status = TreatmentPlanStatus.InProgress;
    public void Complete() => Status = TreatmentPlanStatus.Completed;
}
