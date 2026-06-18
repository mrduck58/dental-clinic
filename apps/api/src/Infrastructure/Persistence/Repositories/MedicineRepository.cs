using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Infrastructure.Persistence.Repositories;

public class MedicineRepository(AppDbContext db) : IMedicineRepository
{
    public async Task<IEnumerable<Medicine>> GetAllAsync(CancellationToken ct = default)
        => await db.Medicines
            .OrderBy(m => m.Name)
            .ToListAsync(ct);

    public async Task<Medicine?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Medicines.FirstOrDefaultAsync(m => m.Id == id, ct);

    public async Task AddAsync(Medicine medicine, CancellationToken ct = default)
    {
        await db.Medicines.AddAsync(medicine, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Medicine medicine, CancellationToken ct = default)
    {
        db.Medicines.Update(medicine);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Medicine medicine, CancellationToken ct = default)
    {
        db.Medicines.Remove(medicine);
        await db.SaveChangesAsync(ct);
    }
}
