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
    int RescheduleCount);

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
                RescheduleCount: 0);
        }

        var targetPatientId = request.PatientId ?? primaryPatient.Id;
        var now = DateTimeOffset.UtcNow;

        var activeCount = await appointmentRepository.CountActiveAppointmentsForUserAsync(request.UserId, null, ct);
        var cooldownUntil = await appointmentRepository.GetPatientCooldownUntilAsync(targetPatientId, now, ct);
        var cancelCount = await appointmentRepository.GetPatientCancellationCountAsync(targetPatientId, ct);
        var rescheduleCount = await appointmentRepository.GetPatientRescheduleCountAsync(targetPatientId, ct);

        var isInCooldown = cooldownUntil.HasValue && cooldownUntil.Value > now;
        var cooldownRemaining = isInCooldown ? (int)Math.Max(0, (cooldownUntil!.Value - now).TotalSeconds) : 0;
        var canBookNew = activeCount < 2 && !isInCooldown;

        return new BookingEligibilityDto(
            ActiveBookingCount: activeCount,
            MaxActiveBookings: 2,
            CanBookNew: canBookNew,
            IsInCooldown: isInCooldown,
            CooldownRemainingSeconds: cooldownRemaining,
            CancellationCount: cancelCount,
            RescheduleCount: rescheduleCount);
    }
}
