using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Infrastructure.Persistence.Repositories;

public class TreatmentProcedureRepository(AppDbContext db) : ITreatmentProcedureRepository
{
    public async Task<IEnumerable<TreatmentProcedure>> GetByServiceIdAsync(Guid serviceId, CancellationToken ct = default)
        => await db.TreatmentProcedures
            .AsNoTracking()
            .Where(p => p.ServiceId == serviceId)
            .OrderBy(p => p.StepNumber)
            .ToListAsync(ct);

    public async Task ReplaceAllForServiceAsync(Guid serviceId, IEnumerable<TreatmentProcedure> newProcedures, CancellationToken ct = default)
    {
        var existing = await db.TreatmentProcedures
            .Where(p => p.ServiceId == serviceId)
            .ToListAsync(ct);
        db.TreatmentProcedures.RemoveRange(existing);

        db.TreatmentProcedures.AddRange(newProcedures);

        await db.SaveChangesAsync(ct);
    }
}
