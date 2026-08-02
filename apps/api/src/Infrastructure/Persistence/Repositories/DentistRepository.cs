using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Infrastructure.Persistence.Repositories;

public class DentistRepository(AppDbContext db) : IDentistRepository
{
    public async Task<Dentist?> GetByIdOrUserIdAsync(Guid idOrUserId, CancellationToken ct = default)
        => await db.Dentists
            .AsNoTracking()
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.Id == idOrUserId || d.UserId == idOrUserId, ct);

    public async Task<Dentist?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => await db.Dentists.FirstOrDefaultAsync(d => d.UserId == userId, ct);

    public async Task<Dentist?> GetByUserIdWithUserAsync(Guid userId, CancellationToken ct = default)
        => await db.Dentists
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.UserId == userId, ct);

    public async Task<List<Dentist>> GetAllWithUserAsync(CancellationToken ct = default)
        => await db.Dentists
            .AsNoTracking()
            .Include(d => d.User)
            .ToListAsync(ct);

    public async Task<Guid?> GetUserIdByDentistIdAsync(Guid dentistId, CancellationToken ct = default)
        => await db.Dentists
            .Where(d => d.Id == dentistId)
            .Select(d => (Guid?)d.UserId)
            .FirstOrDefaultAsync(ct);

    public async Task AddAsync(Dentist dentist, CancellationToken ct = default)
    {
        await db.Dentists.AddAsync(dentist, ct);
        await db.SaveChangesAsync(ct);
    }
}
