using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Infrastructure.Persistence.Repositories;

public class SupplyTransactionRepository(AppDbContext db) : ISupplyTransactionRepository
{
    public async Task<IEnumerable<SupplyTransaction>> GetAllAsync(CancellationToken ct = default)
        => await db.SupplyTransactions
            .Include(t => t.SupplyItem)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);

    public async Task AddAsync(SupplyTransaction transaction, CancellationToken ct = default)
    {
        await db.SupplyTransactions.AddAsync(transaction, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task AddImportAsync(SupplyItem? newItem, SupplyTransaction transaction, CancellationToken ct = default)
    {
        if (newItem is not null)
            await db.SupplyItems.AddAsync(newItem, ct);

        await db.SupplyTransactions.AddAsync(transaction, ct);
        await db.SaveChangesAsync(ct);
    }
}
