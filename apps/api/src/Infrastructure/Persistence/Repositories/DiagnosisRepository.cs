using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Infrastructure.Persistence.Repositories;

public class DiagnosisRepository(AppDbContext db) : IDiagnosisRepository
{
    public async Task<Diagnosis?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Diagnoses.FirstOrDefaultAsync(d => d.Id == id, ct);

    public async Task AddAsync(Diagnosis diagnosis, CancellationToken ct = default)
    {
        await db.Diagnoses.AddAsync(diagnosis, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Diagnosis diagnosis, CancellationToken ct = default)
    {
        db.Diagnoses.Update(diagnosis);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Diagnosis diagnosis, CancellationToken ct = default)
    {
        db.Diagnoses.Remove(diagnosis);
        await db.SaveChangesAsync(ct);
    }
}
