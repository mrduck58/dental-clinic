using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Infrastructure.Persistence.Repositories;

public class SupplyTransactionRepository(AppDbContext db) : ISupplyTransactionRepository
{
    public async Task<IEnumerable<SupplyTransaction>> GetAllAsync(Guid? roomId = null, CancellationToken ct = default)
        => await db.SupplyTransactions
            .Include(t => t.SupplyItem)
            .Include(t => t.Room)
            .Where(t => roomId == null || t.RoomId == roomId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);

    public async Task<IEnumerable<SupplyTransaction>> GetImportsInRangeAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken ct = default)
        => await db.SupplyTransactions
            .AsNoTracking()
            .Include(t => t.SupplyItem)
            .Where(t => t.Type == "import" && t.UnitPrice != null && t.CreatedAt >= start && t.CreatedAt < end)
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
