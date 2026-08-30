namespace DentalClinic.API.Domain.Entities;

using DentalClinic.API.Domain.Enums;

/// <summary>
/// Đại diện cho một phiên / bước điều trị thực tế của bệnh nhân thuộc về một TreatmentPlanItem.
/// </summary>
public class TreatmentSession
{
    public Guid Id { get; private set; }
    public Guid TreatmentPlanItemId { get; private set; }
    public Guid? TreatmentProcedureId { get; private set; }
    public int SessionNumber { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public TreatmentSessionStatus Status { get; private set; }
    public int DurationMinutes { get; private set; } = 30;
    public Guid? DentistId { get; private set; }
    public DateTimeOffset? PerformedAt { get; private set; }
    public string? Note { get; private set; }
    public int Percent { get; private set; } = 0;
    public DateTimeOffset CreatedAt { get; private set; }

    // Navigation properties
    public TreatmentPlanItem TreatmentPlanItem { get; private set; } = null!;
    public TreatmentProcedure? TreatmentProcedure { get; private set; }
    public DentistProfile? Dentist { get; private set; }
    public ICollection<AppointmentSession> AppointmentSessions { get; private set; } = new List<AppointmentSession>();
    public ICollection<TreatmentSupplyUsage> SupplyUsages { get; private set; } = new List<TreatmentSupplyUsage>();
    public ICollection<FollowUp> FollowUps { get; private set; } = new List<FollowUp>();
    public ICollection<StepProgressEntry> StepProgressEntries { get; private set; } = new List<StepProgressEntry>();

    private TreatmentSession() { }

    public static TreatmentSession Create(
        Guid treatmentPlanItemId,
        int sessionNumber,
        string name,
        int durationMinutes = 30,
        Guid? treatmentProcedureId = null,
        Guid? dentistId = null,
        string? note = null)
    {
        return new TreatmentSession
        {
            Id = Guid.NewGuid(),
            TreatmentPlanItemId = treatmentPlanItemId,
            SessionNumber = sessionNumber,
            Name = name,
            DurationMinutes = durationMinutes > 0 ? durationMinutes : 30,
            TreatmentProcedureId = treatmentProcedureId,
            DentistId = dentistId,
            Note = note,
            Status = TreatmentSessionStatus.Planned,
            Percent = 0,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Update(string name, int durationMinutes, string? note)
    {
        Name = name;
        DurationMinutes = durationMinutes > 0 ? durationMinutes : 30;
        Note = note;
    }

    public void SetSessionNumber(int sessionNumber)
    {
        SessionNumber = sessionNumber;
    }

    public void SetStatus(TreatmentSessionStatus status, DateTimeOffset? performedAt = null, int percent = 0, Guid? dentistId = null, string? note = null)
    {
        Status = status;
        Percent = percent > 0 ? percent : (status == TreatmentSessionStatus.Completed ? 100 : 0);
        if (status == TreatmentSessionStatus.Completed)
        {
            PerformedAt = performedAt ?? DateTimeOffset.UtcNow;
        }
        else if (status == TreatmentSessionStatus.Planned)
        {
            PerformedAt = null;
        }
        if (dentistId.HasValue) DentistId = dentistId;
        if (note != null) Note = note;
    }
}
