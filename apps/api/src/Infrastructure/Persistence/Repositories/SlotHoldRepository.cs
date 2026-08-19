using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Infrastructure.Persistence.Repositories;

public class SlotHoldRepository(AppDbContext dbContext) : ISlotHoldRepository
{
    public async Task<AppointmentSlotHold?> GetActiveHoldForSlotAsync(
        Guid dentistId,
        DateTimeOffset appointmentDate,
        DateTimeOffset now,
        CancellationToken ct = default)
    {
        return await dbContext.AppointmentSlotHolds
            .Where(h => h.DentistId == dentistId
                     && h.AppointmentDate == appointmentDate
                     && h.Status == AppointmentSlotHold.StatusHeld
                     && h.ExpiresAt > now)
            .OrderByDescending(h => h.CreatedAt)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<AppointmentSlotHold?> GetActiveHoldForPatientAsync(
        Guid patientId,
        DateTimeOffset now,
        CancellationToken ct = default)
    {
        return await dbContext.AppointmentSlotHolds
            .Where(h => h.PatientId == patientId
                     && h.Status == AppointmentSlotHold.StatusHeld
                     && h.ExpiresAt > now)
            .OrderByDescending(h => h.CreatedAt)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<AppointmentSlotHold>> GetActiveHoldsForUserOrPatientAsync(
        Guid userId,
        Guid patientId,
        DateTimeOffset now,
        CancellationToken ct = default)
    {
        return await dbContext.AppointmentSlotHolds
            .Where(h => ((userId != Guid.Empty && h.UserId == userId) || (patientId != Guid.Empty && h.PatientId == patientId))
                     && h.Status == AppointmentSlotHold.StatusHeld
                     && h.ExpiresAt > now)
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<AppointmentSlotHold>> GetActiveHoldsForDentistAndDateAsync(
        Guid dentistId,
        DateOnly date,
        DateTimeOffset now,
        CancellationToken ct = default)
    {
        var startUtc = new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(7)).ToUniversalTime();
        var endUtc = startUtc.AddDays(1);

        return await dbContext.AppointmentSlotHolds
            .Where(h => h.DentistId == dentistId
                     && h.AppointmentDate >= startUtc
                     && h.AppointmentDate < endUtc
                     && h.Status == AppointmentSlotHold.StatusHeld
                     && h.ExpiresAt > now)
            .ToListAsync(ct);
    }

    public async Task<int> GetFailedHoldCountTodayAsync(
        Guid patientId,
        DateTimeOffset now,
        CancellationToken ct = default)
    {
        var vnNow = now.ToOffset(TimeSpan.FromHours(7));
        var startOfVnToday = new DateTimeOffset(vnNow.Year, vnNow.Month, vnNow.Day, 0, 0, 0, TimeSpan.FromHours(7)).ToUniversalTime();

        return await dbContext.AppointmentSlotHolds
            .CountAsync(h => h.PatientId == patientId
                          && h.CreatedAt >= startOfVnToday
                          && !h.IsSuccess
                          && (h.Status == AppointmentSlotHold.StatusExpired
                              || (h.Status == AppointmentSlotHold.StatusHeld && h.ExpiresAt <= now)),
                        ct);
    }

    public async Task<IReadOnlyList<AppointmentSlotHold>> GetExpiredActiveHoldsAsync(
        DateTimeOffset now,
        CancellationToken ct = default)
    {
        return await dbContext.AppointmentSlotHolds
            .Where(h => h.Status == AppointmentSlotHold.StatusHeld && h.ExpiresAt <= now)
            .ToListAsync(ct);
    }

    public async Task AddAsync(AppointmentSlotHold hold, CancellationToken ct = default)
    {
        await dbContext.AppointmentSlotHolds.AddAsync(hold, ct);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(AppointmentSlotHold hold, CancellationToken ct = default)
    {
        dbContext.AppointmentSlotHolds.Update(hold);
        await dbContext.SaveChangesAsync(ct);
    }
}
