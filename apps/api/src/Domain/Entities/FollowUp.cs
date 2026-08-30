namespace DentalClinic.API.Domain.Entities;

using DentalClinic.API.Domain.Enums;

/// <summary>
/// Chỉ định tái khám y khoa độc lập do Bác sĩ lập sau buổi khám hoặc bước điều trị.
/// </summary>
public class FollowUp
{
    public Guid Id { get; private set; }
    public Guid PatientId { get; private set; }
    public Guid DentistId { get; private set; }
    public Guid OriginAppointmentId { get; private set; }
    public Guid? TreatmentPlanItemId { get; private set; }
    public Guid? TreatmentSessionId { get; private set; }
    public DateOnly DueDate { get; private set; }
    public string? Note { get; private set; }
    public FollowUpStatus Status { get; private set; }
    public Guid? AppointmentId { get; private set; } // Lịch hẹn thực tế đã đặt hoặc check-in cho lần tái khám này
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    // Navigation properties
    public Patient Patient { get; private set; } = null!;
    public DentistProfile Dentist { get; private set; } = null!;
    public Appointment OriginAppointment { get; private set; } = null!;
    public TreatmentPlanItem? TreatmentPlanItem { get; private set; }
    public TreatmentSession? TreatmentSession { get; private set; }
    public Appointment? Appointment { get; private set; }

    private FollowUp() { }

    public static FollowUp Create(
        Guid patientId,
        Guid dentistId,
        Guid originAppointmentId,
        DateOnly dueDate,
        string? note = null,
        Guid? treatmentPlanItemId = null,
        Guid? treatmentSessionId = null)
    {
        return new FollowUp
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            DentistId = dentistId,
            OriginAppointmentId = originAppointmentId,
            DueDate = dueDate,
            Note = note,
            TreatmentPlanItemId = treatmentPlanItemId,
            TreatmentSessionId = treatmentSessionId,
            Status = FollowUpStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Update(DateOnly dueDate, string? note, Guid? treatmentPlanItemId = null, Guid? treatmentSessionId = null)
    {
        DueDate = dueDate;
        Note = note;
        TreatmentPlanItemId = treatmentPlanItemId;
        TreatmentSessionId = treatmentSessionId;
    }

    public void LinkAppointment(Guid appointmentId)
    {
        AppointmentId = appointmentId;
        Status = FollowUpStatus.Booked;
    }

    public void Complete(DateTimeOffset? completedAt = null)
    {
        Status = FollowUpStatus.Completed;
        CompletedAt = completedAt ?? DateTimeOffset.UtcNow;
    }

    public void Cancel()
    {
        Status = FollowUpStatus.Cancelled;
    }
}
