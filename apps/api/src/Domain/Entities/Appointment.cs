using DentalClinic.API.Domain.Enums;

namespace DentalClinic.API.Domain.Entities;

public class Appointment
{
    public Guid Id { get; private set; }
    public Guid PatientId { get; private set; }
    public Guid DentistId { get; private set; }
    public DateTimeOffset AppointmentDate { get; private set; }
    public AppointmentStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    // Navigation properties
    public Patient Patient { get; private set; } = null!;
    public Dentist Dentist { get; private set; } = null!;
    public Invoice? Invoice { get; private set; }
    public MedicalRecord? MedicalRecord { get; private set; }

    private Appointment() { }

    public static Appointment Create(Guid patientId, Guid dentistId, DateTimeOffset appointmentDate, string? notes = null)
    {
        return new Appointment
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            DentistId = dentistId,
            AppointmentDate = appointmentDate,
            Status = AppointmentStatus.Pending,
            Notes = notes,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Confirm() => Status = AppointmentStatus.Confirmed;
    public void Complete() => Status = AppointmentStatus.Completed;
    public void Cancel() => Status = AppointmentStatus.Cancelled;
}
