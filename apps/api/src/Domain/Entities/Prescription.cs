namespace DentalClinic.API.Domain.Entities;

public class Prescription
{
    public Guid Id { get; private set; }
    public Guid AppointmentId { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    // Navigation property
    public Appointment Appointment { get; private set; } = null!;
    public ICollection<PrescriptionItem> Items { get; private set; } = new List<PrescriptionItem>();

    private Prescription() { }

    public static Prescription Create(
        Guid appointmentId,
        string? notes = null)
    {
        return new Prescription
        {
            Id = Guid.NewGuid(),
            AppointmentId = appointmentId,
            Notes = notes,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void UpdateNotes(string? notes)
    {
        Notes = notes;
    }
}
