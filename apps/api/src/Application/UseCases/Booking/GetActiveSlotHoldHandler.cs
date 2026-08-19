using DentalClinic.API.Domain.Interfaces.Repositories;

namespace DentalClinic.API.Application.UseCases.Booking;

public record ActiveSlotHoldDto(
    Guid HoldId,
    Guid PatientId,
    Guid DentistId,
    DateTimeOffset AppointmentDate,
    string TimeSlot,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    int RemainingSeconds,
    int FailedHoldsToday);

public class GetActiveSlotHoldHandler(ISlotHoldRepository slotHoldRepository)
{
    public async Task<ActiveSlotHoldDto?> Handle(Guid patientId, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var hold = await slotHoldRepository.GetActiveHoldForPatientAsync(patientId, now, ct);
        var failedCount = await slotHoldRepository.GetFailedHoldCountTodayAsync(patientId, now, ct);

        if (hold == null)
            return null;

        var remaining = (int)Math.Max(0, (hold.ExpiresAt - now).TotalSeconds);
        return new ActiveSlotHoldDto(
            hold.Id,
            hold.PatientId,
            hold.DentistId,
            hold.AppointmentDate,
            hold.TimeSlot,
            hold.CreatedAt,
            hold.ExpiresAt,
            remaining,
            failedCount);
    }
}
