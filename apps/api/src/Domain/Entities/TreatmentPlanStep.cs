namespace DentalClinic.API.Domain.Entities;

using DentalClinic.API.Domain.Enums;

/// <summary>
/// Đại diện cho một giai đoạn / bước điều trị trong phác đồ của bệnh nhân thuộc về một TreatmentPlanItem.
/// </summary>
public class TreatmentPlanStep
{
    public Guid Id { get; private set; }
    public Guid TreatmentPlanItemId { get; private set; }
    public Guid? TreatmentProcedureId { get; private set; }
    public int StepNumber { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public int EstimatedSessionCount { get; private set; } = 1;
    public int DurationMinutes { get; private set; } = 30;
    public int Percent { get; private set; } = 0;
    public TreatmentPlanStepStatus Status { get; private set; } = TreatmentPlanStepStatus.Planned;
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    // Navigation properties
    public TreatmentPlanItem TreatmentPlanItem { get; private set; } = null!;
    public TreatmentProcedure? TreatmentProcedure { get; private set; }
    public ICollection<TreatmentSession> Sessions { get; private set; } = new List<TreatmentSession>();
    public ICollection<FollowUp> FollowUps { get; private set; } = new List<FollowUp>();

    private TreatmentPlanStep() { }

    public static TreatmentPlanStep Create(
        Guid treatmentPlanItemId,
        int stepNumber,
        string name,
        int estimatedSessionCount = 1,
        int durationMinutes = 30,
        Guid? treatmentProcedureId = null,
        string? description = null,
        string? notes = null)
    {
        return new TreatmentPlanStep
        {
            Id = Guid.NewGuid(),
            TreatmentPlanItemId = treatmentPlanItemId,
            StepNumber = stepNumber,
            Name = name,
            EstimatedSessionCount = estimatedSessionCount > 0 ? estimatedSessionCount : 1,
            DurationMinutes = durationMinutes > 0 ? durationMinutes : 30,
            TreatmentProcedureId = treatmentProcedureId,
            Description = description,
            Notes = notes,
            Percent = 0,
            Status = TreatmentPlanStepStatus.Planned,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Update(string name, int estimatedSessionCount, int durationMinutes, string? description, string? notes)
    {
        Name = name;
        EstimatedSessionCount = estimatedSessionCount > 0 ? estimatedSessionCount : 1;
        DurationMinutes = durationMinutes > 0 ? durationMinutes : 30;
        Description = description;
        Notes = notes;
    }

    public void SetStepNumber(int stepNumber)
    {
        StepNumber = stepNumber;
    }

    public void SetPercent(int percent)
    {
        Percent = Math.Clamp(percent, 0, 100);
        if (Percent >= 100 && Status != TreatmentPlanStepStatus.Completed)
        {
            Status = TreatmentPlanStepStatus.Completed;
            CompletedAt = DateTimeOffset.UtcNow;
        }
        else if (Percent > 0 && Status == TreatmentPlanStepStatus.Planned)
        {
            Status = TreatmentPlanStepStatus.InProgress;
            CompletedAt = null;
        }
        else if (Percent == 0 && Status == TreatmentPlanStepStatus.Completed)
        {
            Status = TreatmentPlanStepStatus.Planned;
            CompletedAt = null;
        }
    }

    public void SetStatus(TreatmentPlanStepStatus status, DateTimeOffset? completedAt = null)
    {
        Status = status;
        if (status == TreatmentPlanStepStatus.Completed)
        {
            Percent = 100;
            CompletedAt = completedAt ?? DateTimeOffset.UtcNow;
        }
        else if (status == TreatmentPlanStepStatus.Planned)
        {
            Percent = 0;
            CompletedAt = null;
        }
    }

    public void RecalculateProgressFromSessions()
    {
        var completedSessions = Sessions.Count(s => s.Status == TreatmentSessionStatus.Completed);
        int totalExpected = Math.Max(EstimatedSessionCount, Sessions.Count);
        if (totalExpected == 0) totalExpected = 1;

        int calculatedPercent = Math.Min(100, (int)Math.Round((double)completedSessions / totalExpected * 100));
        SetPercent(calculatedPercent);
    }
}
