using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Infrastructure.Persistence.Repositories;

public class ServiceSupplyItemRepository(AppDbContext db) : IServiceSupplyItemRepository
{
    public async Task<IEnumerable<ServiceSupplyItem>> GetByServiceIdAsync(Guid serviceId, CancellationToken ct = default)
        => await db.ServiceSupplyItems
            .AsNoTracking()
            .Include(s => s.SupplyItem)
            .Where(s => s.ServiceId == serviceId)
            .OrderBy(s => s.SupplyItem.Name)
            .ToListAsync(ct);

    public async Task<IEnumerable<ServiceSupplyItem>> GetEffectiveByServiceIdAsync(Guid serviceId, string? optionName, CancellationToken ct = default)
        => await db.ServiceSupplyItems
            .AsNoTracking()
            .Include(s => s.SupplyItem)
            .Where(s => s.ServiceId == serviceId
                && (s.ServiceOptionName == null || (optionName != null && s.ServiceOptionName == optionName)))
            .OrderBy(s => s.SupplyItem.Name)
            .ToListAsync(ct);

    public async Task ReplaceAllForServiceAsync(Guid serviceId, IEnumerable<ServiceSupplyItem> newItems, CancellationToken ct = default)
    {
        var existing = await db.ServiceSupplyItems
            .Where(s => s.ServiceId == serviceId)
            .ToListAsync(ct);
        db.ServiceSupplyItems.RemoveRange(existing);

        db.ServiceSupplyItems.AddRange(newItems);

        await db.SaveChangesAsync(ct);
    }
}
