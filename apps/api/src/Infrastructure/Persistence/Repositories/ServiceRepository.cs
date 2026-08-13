using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Infrastructure.Persistence.Repositories;

public class ServiceRepository(AppDbContext db) : IServiceRepository
{
    public async Task<IEnumerable<Service>> GetAllAsync(CancellationToken ct = default)
        => await db.Services
            .Include(s => s.Options)
            .OrderBy(s => s.Name)
            .ToListAsync(ct);

    public async Task<Service?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Services
            .Include(s => s.Options)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task AddAsync(Service service, CancellationToken ct = default)
    {
        await db.Services.AddAsync(service, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Service service, CancellationToken ct = default)
    {
        db.Services.Update(service);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Service service, CancellationToken ct = default)
    {
        db.Services.Remove(service);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<Service>> GetActiveAsync(CancellationToken ct = default)
        => await db.Services
            .Include(s => s.Options)
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .ToListAsync(ct);

    public async Task DeleteOptionsAsync(Guid serviceId, CancellationToken ct = default)
    {
        var options = await db.ServiceOptions
            .Where(o => o.ServiceId == serviceId)
            .ToListAsync(ct);
        db.ServiceOptions.RemoveRange(options);
        await db.SaveChangesAsync(ct);
    }
}
