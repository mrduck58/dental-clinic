using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Infrastructure.Persistence.Repositories;

public class AiUsageLogRepository(AppDbContext db) : IAiUsageLogRepository
{
    public async Task AddAsync(AiUsageLog log, CancellationToken ct = default)
    {
        await db.AiUsageLogs.AddAsync(log, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<AiUsageLog>> GetSinceAsync(DateTimeOffset since, CancellationToken ct = default)
        => await db.AiUsageLogs.Where(l => l.CreatedAt >= since).ToListAsync(ct);
}
