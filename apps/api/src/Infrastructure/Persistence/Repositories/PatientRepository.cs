using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Infrastructure.Persistence.Repositories;

public class PatientRepository(AppDbContext dbContext) : IPatientRepository
{
    public async Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Patients
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Patient?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Patients
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
    }

    public async Task<IReadOnlyList<Patient>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Patients
            .Include(p => p.User)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Patient patient, CancellationToken cancellationToken = default)
    {
        await dbContext.Patients.AddAsync(patient, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Patient patient, CancellationToken cancellationToken = default)
    {
        dbContext.Patients.Update(patient);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Patient patient, CancellationToken cancellationToken = default)
    {
        dbContext.Patients.Remove(patient);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Patient>> GetFamilyMembersAsync(Guid primaryPatientId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Patients
            .Include(p => p.User)
            .Where(p => p.PrimaryPatientId == primaryPatientId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Patient>> SearchAsync(string term, int limit, CancellationToken cancellationToken = default)
    {
        var needle = term.Trim().ToLower();
        if (needle.Length == 0) return [];

        return await dbContext.Patients
            .Include(p => p.User)
            .Where(p =>
                (p.User.FullName != null && p.User.FullName.ToLower().Contains(needle)) ||
                (p.PhoneNumber != null && p.PhoneNumber.Contains(needle)) ||
                (p.User.PhoneNumber != null && p.User.PhoneNumber.Contains(needle)))
            .OrderBy(p => p.User.FullName)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
}
