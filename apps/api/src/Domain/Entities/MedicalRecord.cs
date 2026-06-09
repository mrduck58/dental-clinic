namespace DentalClinic.API.Domain.Entities;

public class MedicalRecord
{
    public Guid Id { get; private set; }
    public Guid PatientId { get; private set; }
    public Guid DentistId { get; private set; }
    public Guid? AppointmentId { get; private set; }
    public string Diagnosis { get; private set; } = string.Empty;
    public string TreatmentPlan { get; private set; } = string.Empty;
    public string? Notes { get; private set; }
    public DateOnly VisitDate { get; private set; }

    // Navigation properties
    public Patient Patient { get; private set; } = null!;
    public Dentist Dentist { get; private set; } = null!;
    public Appointment? Appointment { get; private set; }

    private MedicalRecord() { }

    public static MedicalRecord Create(
        Guid patientId, Guid dentistId, string diagnosis,
        string treatmentPlan, Guid? appointmentId = null, string? notes = null)
    {
        return new MedicalRecord
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            DentistId = dentistId,
            AppointmentId = appointmentId,
            Diagnosis = diagnosis,
            TreatmentPlan = treatmentPlan,
            Notes = notes,
            VisitDate = DateOnly.FromDateTime(DateTime.UtcNow)
        };
    }
}
