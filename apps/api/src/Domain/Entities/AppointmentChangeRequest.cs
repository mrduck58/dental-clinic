using DentalClinic.API.Domain.Enums;

namespace DentalClinic.API.Domain.Entities;

public class AppointmentChangeRequest
{
    public Guid Id { get; private set; }
    public Guid AppointmentId { get; private set; }
    public Guid PatientId { get; private set; }
    public Guid RequestedByUserId { get; private set; }
    public AppointmentChangeType Type { get; private set; }
    public AppointmentChangeRequestStatus Status { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public DateTimeOffset? DesiredDate { get; private set; }
    public string? DesiredTimeSlot { get; private set; }
    public Guid? DesiredDentistId { get; private set; }
    public string? StaffNote { get; private set; }
    public Guid? ProcessedByUserId { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    // Navigation properties
    public Appointment Appointment { get; private set; } = null!;
    public Patient Patient { get; private set; } = null!;
    public User RequestedByUser { get; private set; } = null!;
    public DentistProfile? DesiredDentist { get; private set; }
    public User? ProcessedByUser { get; private set; }

    private AppointmentChangeRequest() { }

    public static AppointmentChangeRequest CreateCancelRequest(
        Guid appointmentId,
        Guid patientId,
        Guid requestedByUserId,
        string reason)
    {
        return new AppointmentChangeRequest
        {
            Id = Guid.NewGuid(),
            AppointmentId = appointmentId,
            PatientId = patientId,
            RequestedByUserId = requestedByUserId,
            Type = AppointmentChangeType.Cancel,
            Status = AppointmentChangeRequestStatus.Pending,
            Reason = reason,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public static AppointmentChangeRequest CreateRescheduleRequest(
        Guid appointmentId,
        Guid patientId,
        Guid requestedByUserId,
        string reason,
        DateTimeOffset? desiredDate = null,
        string? desiredTimeSlot = null,
        Guid? desiredDentistId = null)
    {
        return new AppointmentChangeRequest
        {
            Id = Guid.NewGuid(),
            AppointmentId = appointmentId,
            PatientId = patientId,
            RequestedByUserId = requestedByUserId,
            Type = AppointmentChangeType.Reschedule,
            Status = AppointmentChangeRequestStatus.Pending,
            Reason = reason,
            DesiredDate = desiredDate,
            DesiredTimeSlot = desiredTimeSlot,
            DesiredDentistId = desiredDentistId,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Approve(Guid staffUserId, string? staffNote = null)
    {
        Status = AppointmentChangeRequestStatus.Approved;
        ProcessedByUserId = staffUserId;
        ProcessedAt = DateTimeOffset.UtcNow;
        StaffNote = staffNote;
    }

    public void Reject(Guid staffUserId, string staffNote)
    {
        Status = AppointmentChangeRequestStatus.Rejected;
        ProcessedByUserId = staffUserId;
        ProcessedAt = DateTimeOffset.UtcNow;
        StaffNote = staffNote;
    }
}
