using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Infrastructure.Persistence.Repositories;

public class PromotionRepository(AppDbContext db) : IPromotionRepository
{
    public async Task<IEnumerable<Promotion>> GetAllAsync(CancellationToken ct = default) =>
        await db.Promotions.OrderByDescending(p => p.CreatedAt).ToListAsync(ct);

    public async Task<Promotion?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.Promotions.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task AddAsync(Promotion promotion, CancellationToken ct = default)
    {
        await db.Promotions.AddAsync(promotion, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Promotion promotion, CancellationToken ct = default)
    {
        db.Promotions.Update(promotion);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Promotion promotion, CancellationToken ct = default)
    {
        db.Promotions.Remove(promotion);
        await db.SaveChangesAsync(ct);
    }
}
