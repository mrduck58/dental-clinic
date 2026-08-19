namespace DentalClinic.API.Domain.Entities;

/// <summary>
/// Quản lý việc giữ tạm thời một ca khám (Slot Hold) trong tối đa 5 phút.
/// </summary>
public class AppointmentSlotHold
{
    public const string StatusHeld = "Held";
    public const string StatusConfirmed = "Confirmed";
    public const string StatusReleased = "Released";
    public const string StatusExpired = "Expired";

    public Guid Id { get; private set; }
    public Guid PatientId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid DentistId { get; private set; }
    public DateTimeOffset AppointmentDate { get; private set; }
    public string TimeSlot { get; private set; } = string.Empty;
    public Guid? ServiceId { get; private set; }
    public int DurationMinutes { get; private set; } = 30;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public string Status { get; private set; } = StatusHeld;
    public bool IsSuccess { get; private set; }

    private AppointmentSlotHold() { }

    public static AppointmentSlotHold Create(
        Guid patientId,
        Guid userId,
        Guid dentistId,
        DateTimeOffset appointmentDate,
        string timeSlot,
        DateTimeOffset now,
        Guid? serviceId = null,
        int durationMinutes = 30)
    {
        return CreateWithExpiry(
            patientId,
            userId,
            dentistId,
            appointmentDate,
            timeSlot,
            now,
            now.AddMinutes(5),
            serviceId,
            durationMinutes);
    }

    public static AppointmentSlotHold CreateWithExpiry(
        Guid patientId,
        Guid userId,
        Guid dentistId,
        DateTimeOffset appointmentDate,
        string timeSlot,
        DateTimeOffset now,
        DateTimeOffset expiresAt,
        Guid? serviceId = null,
        int durationMinutes = 30)
    {
        return new AppointmentSlotHold
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            UserId = userId,
            DentistId = dentistId,
            AppointmentDate = appointmentDate,
            TimeSlot = timeSlot,
            CreatedAt = now,
            ExpiresAt = expiresAt,
            Status = StatusHeld,
            IsSuccess = false,
            ServiceId = serviceId,
            DurationMinutes = durationMinutes > 0 ? durationMinutes : 30,
        };
    }

    public void Confirm()
    {
        Status = StatusConfirmed;
        IsSuccess = true;
    }

    public void Release()
    {
        Status = StatusReleased;
        IsSuccess = false;
    }

    public void MarkExpired()
    {
        Status = StatusExpired;
        IsSuccess = false;
    }

    public bool IsActive(DateTimeOffset now) => Status == StatusHeld && ExpiresAt > now;
}
