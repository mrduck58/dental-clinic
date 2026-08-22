using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Booking;

public record BookingEligibilityDto(
    int ActiveBookingCount,
    int MaxActiveBookings,
    bool CanBookNew,
    bool IsInCooldown,
    int CooldownRemainingSeconds,
    int CancellationCount,
    int RescheduleCount,
    bool IsInRescheduleCooldown = false,
    int RescheduleCooldownRemainingSeconds = 0);

public record GetBookingEligibilityQuery(Guid UserId, Guid? PatientId) : IRequest<BookingEligibilityDto>;

public class GetBookingEligibilityHandler(
    IPatientRepository patientRepository,
    IAppointmentRepository appointmentRepository) : IRequestHandler<GetBookingEligibilityQuery, BookingEligibilityDto>
{
    public async Task<BookingEligibilityDto> Handle(GetBookingEligibilityQuery request, CancellationToken ct)
    {
        var primaryPatient = await patientRepository.GetByUserIdAsync(request.UserId, ct);
        if (primaryPatient is null)
        {
            return new BookingEligibilityDto(
                ActiveBookingCount: 0,
                MaxActiveBookings: 2,
                CanBookNew: true,
                IsInCooldown: false,
                CooldownRemainingSeconds: 0,
                CancellationCount: 0,
                RescheduleCount: 0,
                IsInRescheduleCooldown: false,
                RescheduleCooldownRemainingSeconds: 0);
        }

        var targetPatientId = request.PatientId ?? primaryPatient.Id;
        var now = DateTimeOffset.UtcNow;
        var vnOffset = TimeSpan.FromHours(7);
        var nowVn = now.ToOffset(vnOffset);
        var startOfTodayUtc = new DateTimeOffset(nowVn.Year, nowVn.Month, nowVn.Day, 0, 0, 0, vnOffset).ToUniversalTime();

        var hasActive = await appointmentRepository.HasActiveAppointmentForPatientAsync(targetPatientId, null, ct);
        var cooldownUntil = await appointmentRepository.GetPatientCooldownUntilAsync(targetPatientId, now, ct);
        var cancelCountToday = await appointmentRepository.GetPatientCancellationCountAsync(targetPatientId, since: startOfTodayUtc, ct);
        var rescheduleCountToday = await appointmentRepository.GetPatientRescheduleCountAsync(targetPatientId, since: startOfTodayUtc, ct);
        var rescheduleCooldownUntil = await appointmentRepository.GetPatientRescheduleCooldownUntilAsync(targetPatientId, now, ct);

        var isInCooldown = cooldownUntil.HasValue && cooldownUntil.Value > now;
        var cooldownRemaining = isInCooldown ? (int)Math.Max(0, (cooldownUntil!.Value - now).TotalSeconds) : 0;
        var isInRescheduleCooldown = rescheduleCooldownUntil.HasValue && rescheduleCooldownUntil.Value > now;
        var rescheduleCooldownRemaining = isInRescheduleCooldown ? (int)Math.Max(0, (rescheduleCooldownUntil!.Value - now).TotalSeconds) : 0;
        var canBookNew = !hasActive && !isInCooldown;

        return new BookingEligibilityDto(
            ActiveBookingCount: hasActive ? 1 : 0,
            MaxActiveBookings: 1,
            CanBookNew: canBookNew,
            IsInCooldown: isInCooldown,
            CooldownRemainingSeconds: cooldownRemaining,
            CancellationCount: cancelCountToday,
            RescheduleCount: rescheduleCountToday,
            IsInRescheduleCooldown: isInRescheduleCooldown,
            RescheduleCooldownRemainingSeconds: rescheduleCooldownRemaining);
    }
}
