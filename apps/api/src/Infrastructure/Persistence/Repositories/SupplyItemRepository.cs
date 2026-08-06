using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Infrastructure.Persistence.Repositories;

public class SupplyItemRepository(AppDbContext db) : ISupplyItemRepository
{
    public async Task<IEnumerable<SupplyItem>> GetAllAsync(CancellationToken ct = default)
        => await db.SupplyItems
            .OrderBy(s => s.Name)
            .ToListAsync(ct);

    public async Task<SupplyItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.SupplyItems.FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task AddAsync(SupplyItem item, CancellationToken ct = default)
    {
        await db.SupplyItems.AddAsync(item, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(SupplyItem item, CancellationToken ct = default)
    {
        db.SupplyItems.Update(item);
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> ExistsByCodeAsync(string code, CancellationToken ct = default)
        => await db.SupplyItems.AnyAsync(s => s.Code == code, ct);

    public async Task<SupplyItem?> GetByNameAsync(string name, CancellationToken ct = default)
        => await db.SupplyItems.FirstOrDefaultAsync(s => s.Name.ToLower() == name.ToLower(), ct);
}
