namespace DentalClinic.API.Domain.Interfaces.Services;

public interface ISlotNotifier
{
    Task NotifySlotHeldAsync(
        Guid dentistId,
        DateOnly date,
        string timeSlot,
        Guid heldByPatientId,
        DateTimeOffset expiresAt,
        CancellationToken ct = default);

    Task NotifySlotReleasedAsync(
        Guid dentistId,
        DateOnly date,
        string timeSlot,
        CancellationToken ct = default);

    Task NotifySlotBookedAsync(
        Guid dentistId,
        DateOnly date,
        string timeSlot,
        CancellationToken ct = default);
}
