using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;

namespace DentalClinic.API.Application.UseCases.Booking;

public record ReleaseSlotHoldCommand(
    Guid? PatientId,
    Guid DentistId,
    DateOnly Date,
    string TimeSlot);

public class ReleaseSlotHoldHandler(
    ISlotHoldRepository slotHoldRepository,
    IPatientRepository patientRepository,
    ICurrentUserService currentUser,
    ISlotNotifier slotNotifier)
{
    public async Task<bool> Handle(ReleaseSlotHoldCommand command, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var userId = currentUser.UserId ?? Guid.Empty;
        var myPatient = userId != Guid.Empty ? await patientRepository.GetByUserIdAsync(userId, ct) : null;
        var targetPatientId = command.PatientId ?? myPatient?.Id ?? Guid.Empty;

        var timePart = command.TimeSlot.Split(" - ")[0].Trim();
        var time = TimeOnly.Parse(timePart);
        var apptDateTime = command.Date.ToDateTime(time);
        var apptDateUtc = new DateTimeOffset(apptDateTime, TimeSpan.FromHours(7)).ToUniversalTime();

        var hold = await slotHoldRepository.GetActiveHoldForSlotAsync(command.DentistId, apptDateUtc, now, ct);
        if (hold == null || (targetPatientId != Guid.Empty && hold.PatientId != targetPatientId))
            return false;

        hold.Release();
        await slotHoldRepository.UpdateAsync(hold, ct);

        await slotNotifier.NotifySlotReleasedAsync(
            command.DentistId,
            command.Date,
            command.TimeSlot,
            ct);

        return true;
    }
}
