using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Infrastructure.Persistence.Repositories;

public class DentistReviewRepository(AppDbContext db) : IDentistReviewRepository
{
    public async Task<List<DentistReview>> GetByDentistIdAsync(Guid dentistId, CancellationToken ct = default)
        => await db.DentistReviews
            .AsNoTracking()
            .Include(r => r.Patient).ThenInclude(p => p.User)
            .Where(r => r.DentistId == dentistId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

    public async Task<DentistReview?> GetByDentistAndPatientAsync(Guid dentistId, Guid patientId, CancellationToken ct = default)
        => await db.DentistReviews
            .FirstOrDefaultAsync(r => r.DentistId == dentistId && r.PatientId == patientId, ct);

    public async Task AddAsync(DentistReview review, CancellationToken ct = default)
    {
        await db.DentistReviews.AddAsync(review, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(DentistReview review, CancellationToken ct = default)
    {
        db.DentistReviews.Update(review);
        await db.SaveChangesAsync(ct);
    }
}
