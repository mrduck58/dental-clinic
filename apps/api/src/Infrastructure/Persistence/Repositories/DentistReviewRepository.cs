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
            .Include(r => r.Appointment).ThenInclude(a => a!.Service)
            .Where(r => r.DentistId == dentistId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<int>> GetRatingsByDentistIdAsync(Guid dentistId, CancellationToken ct = default)
        => await db.DentistReviews
            .AsNoTracking()
            .Where(r => r.DentistId == dentistId)
            .Select(r => r.Rating)
            .ToListAsync(ct);

    public async Task<DentistReview?> GetByDentistAndPatientAsync(Guid dentistId, Guid patientId, CancellationToken ct = default)
        => await db.DentistReviews
            .FirstOrDefaultAsync(r => r.DentistId == dentistId && r.PatientId == patientId, ct);

    public async Task<DentistReview?> GetByAppointmentIdAsync(Guid appointmentId, CancellationToken ct = default)
        => await db.DentistReviews
            .FirstOrDefaultAsync(r => r.AppointmentId == appointmentId, ct);

    public async Task<int> CountByDentistAndPatientAsync(Guid dentistId, Guid patientId, CancellationToken ct = default)
        => await db.DentistReviews.CountAsync(r => r.DentistId == dentistId && r.PatientId == patientId, ct);

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
