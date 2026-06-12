using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Infrastructure.Persistence.Repositories;

public class ServiceRepository(AppDbContext db) : IServiceRepository
{
    public async Task<IEnumerable<Service>> GetAllAsync(CancellationToken ct = default)
        => await db.Services
            .OrderBy(s => s.Category)
            .ThenBy(s => s.Name)
            .ToListAsync(ct);

    public async Task<Service?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Services.FirstOrDefaultAsync(s => s.Id == id, ct);

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
}
