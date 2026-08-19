using DentalClinic.API.Domain.Entities;

namespace DentalClinic.API.Domain.Interfaces.Repositories;

public interface ISlotHoldRepository
{
    Task<AppointmentSlotHold?> GetActiveHoldForSlotAsync(
        Guid dentistId,
        DateTimeOffset appointmentDate,
        DateTimeOffset now,
        CancellationToken ct = default);

    Task<AppointmentSlotHold?> GetActiveHoldForPatientAsync(
        Guid patientId,
        DateTimeOffset now,
        CancellationToken ct = default);

    Task<IReadOnlyList<AppointmentSlotHold>> GetActiveHoldsForDentistAndDateAsync(
        Guid dentistId,
        DateOnly date,
        DateTimeOffset now,
        CancellationToken ct = default);

    Task<int> GetFailedHoldCountTodayAsync(
        Guid patientId,
        DateTimeOffset now,
        CancellationToken ct = default);

    Task<IReadOnlyList<AppointmentSlotHold>> GetExpiredActiveHoldsAsync(
        DateTimeOffset now,
        CancellationToken ct = default);

    Task AddAsync(AppointmentSlotHold hold, CancellationToken ct = default);

    Task UpdateAsync(AppointmentSlotHold hold, CancellationToken ct = default);
}
