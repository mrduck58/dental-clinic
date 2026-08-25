using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Infrastructure.Persistence.Repositories;

public class CommissionRuleRepository(AppDbContext db) : ICommissionRuleRepository
{
    public async Task<IReadOnlyList<CommissionRule>> GetAllAsync(CancellationToken ct = default)
        => await db.CommissionRules.OrderByDescending(r => r.CreatedAt).ToListAsync(ct);

    public async Task<CommissionRule?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.CommissionRules.FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task AddAsync(CommissionRule rule, CancellationToken ct = default)
        => await db.CommissionRules.AddAsync(rule, ct);

    public Task DeleteAsync(CommissionRule rule, CancellationToken ct = default)
    {
        db.CommissionRules.Remove(rule);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await db.SaveChangesAsync(ct);
}
