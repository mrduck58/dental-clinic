using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Infrastructure.Persistence.Repositories;

public class DentistRepository(AppDbContext db) : IDentistRepository
{
    public async Task<DentistProfile?> GetByIdOrUserIdAsync(Guid idOrUserId, CancellationToken ct = default)
        => await db.DentistProfiles
            .AsNoTracking()
            .Include(d => d.Employee).ThenInclude(e => e.User)
            .FirstOrDefaultAsync(d => d.Id == idOrUserId || d.Employee.UserId == idOrUserId, ct);

    public async Task<DentistProfile?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => await db.DentistProfiles.FirstOrDefaultAsync(d => d.Employee.UserId == userId, ct);

    public async Task<DentistProfile?> GetByUserIdWithUserAsync(Guid userId, CancellationToken ct = default)
        => await db.DentistProfiles
            .Include(d => d.Employee).ThenInclude(e => e.User)
            .FirstOrDefaultAsync(d => d.Employee.UserId == userId, ct);

    public async Task<List<DentistProfile>> GetAllWithUserAsync(CancellationToken ct = default)
        => await db.DentistProfiles
            .AsNoTracking()
            .Include(d => d.Employee).ThenInclude(e => e.User)
            .ToListAsync(ct);

    public async Task<Guid?> GetUserIdByDentistIdAsync(Guid dentistProfileId, CancellationToken ct = default)
        => await db.DentistProfiles
            .Where(d => d.Id == dentistProfileId)
            .Select(d => (Guid?)d.Employee.UserId)
            .FirstOrDefaultAsync(ct);

    public async Task AddAsync(DentistProfile dentistProfile, CancellationToken ct = default)
    {
        await db.DentistProfiles.AddAsync(dentistProfile, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(DentistProfile dentistProfile, CancellationToken ct = default)
    {
        db.DentistProfiles.Update(dentistProfile);
        await db.SaveChangesAsync(ct);
    }
}
