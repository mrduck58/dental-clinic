using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace DentalClinic.API.Infrastructure.Persistence.Repositories;

public class TreatmentSupplyUsageRepository(AppDbContext db) : ITreatmentSupplyUsageRepository
{
    public async Task<IEnumerable<TreatmentSupplyUsage>> GetByTreatmentPlanIdAsync(Guid treatmentPlanId, CancellationToken ct = default)
        => await db.TreatmentSupplyUsages
            .AsNoTracking()
            .Include(u => u.SupplyItem)
            .Where(u => u.TreatmentPlanId == treatmentPlanId)
            .OrderBy(u => u.CreatedAt)
            .ToListAsync(ct);

    public async Task<List<TreatmentSupplyUsage>> GetActiveByStepEntryIdAsync(Guid treatmentPlanId, Guid stepEntryId, CancellationToken ct = default)
        => await db.TreatmentSupplyUsages
            .Include(u => u.SupplyItem)
            .Where(u => u.TreatmentPlanId == treatmentPlanId && u.StepEntryId == stepEntryId && !u.IsReversed)
            .ToListAsync(ct);

    public async Task AddAsync(TreatmentSupplyUsage usage, CancellationToken ct = default)
    {
        await db.TreatmentSupplyUsages.AddAsync(usage, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task<ITreatmentSupplyUsageTransaction?> BeginTransactionAsync(CancellationToken ct = default)
    {
        if (!db.Database.IsRelational()) return null;

        var tx = await db.Database.BeginTransactionAsync(ct);
        return new EfTreatmentSupplyUsageTransaction(tx);
    }

    private sealed class EfTreatmentSupplyUsageTransaction(IDbContextTransaction tx) : ITreatmentSupplyUsageTransaction
    {
        public Task CommitAsync(CancellationToken ct = default) => tx.CommitAsync(ct);
        public ValueTask DisposeAsync() => tx.DisposeAsync();
    }
}
