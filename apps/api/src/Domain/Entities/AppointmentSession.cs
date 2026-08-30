namespace DentalClinic.API.Domain.Entities;

/// <summary>
/// Bảng nối liên kết giữa Buổi hẹn (Appointment) và Bước điều trị (TreatmentSession).
/// Cho phép 1 buổi hẹn thực hiện nhiều bước điều trị khác nhau.
/// </summary>
public class AppointmentSession
{
    public Guid Id { get; private set; }
    public Guid AppointmentId { get; private set; }
    public Guid TreatmentSessionId { get; private set; }
    public int Sequence { get; private set; }
    public int DurationMinutes { get; private set; } = 30;
    public string? Note { get; private set; }

    // Navigation properties
    public Appointment Appointment { get; private set; } = null!;
    public TreatmentSession TreatmentSession { get; private set; } = null!;

    private AppointmentSession() { }

    public static AppointmentSession Create(
        Guid appointmentId,
        Guid treatmentSessionId,
        int sequence,
        int durationMinutes = 30,
        string? note = null)
    {
        return new AppointmentSession
        {
            Id = Guid.NewGuid(),
            AppointmentId = appointmentId,
            TreatmentSessionId = treatmentSessionId,
            Sequence = sequence,
            DurationMinutes = durationMinutes > 0 ? durationMinutes : 30,
            Note = note
        };
    }

    public void Update(int sequence, int durationMinutes, string? note)
    {
        Sequence = sequence;
        DurationMinutes = durationMinutes > 0 ? durationMinutes : 30;
        Note = note;
    }
}
