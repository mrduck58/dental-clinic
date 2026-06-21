using DentalClinic.API.Domain.Enums;

namespace DentalClinic.API.Domain.Entities;

public class Diagnosis
{
    public Guid Id { get; private set; }
    public Guid AppointmentId { get; private set; }
    public string DiagnosisCode { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    // Navigation property
    public Appointment Appointment { get; private set; } = null!;

    private Diagnosis() { }

    public static Diagnosis Create(
        Guid appointmentId,
        string diagnosisCode,
        string description,
        string? notes = null)
    {
        return new Diagnosis
        {
            Id = Guid.NewGuid(),
            AppointmentId = appointmentId,
            DiagnosisCode = diagnosisCode,
            Description = description,
            Notes = notes,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Update(string diagnosisCode, string description, string? notes)
    {
        DiagnosisCode = diagnosisCode;
        Description = description;
        Notes = notes;
    }
}
