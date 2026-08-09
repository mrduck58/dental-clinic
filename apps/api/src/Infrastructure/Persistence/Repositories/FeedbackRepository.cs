using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Infrastructure.Persistence.Repositories;

public class FeedbackRepository(AppDbContext db) : IFeedbackRepository
{
    public async Task<IEnumerable<Feedback>> GetAllAsync(CancellationToken ct = default)
        => await db.Feedbacks
            .Include(f => f.Patient).ThenInclude(p => p!.User)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync(ct);

    public async Task<Feedback?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Feedbacks
            .Include(f => f.Patient).ThenInclude(p => p!.User)
            .FirstOrDefaultAsync(f => f.Id == id, ct);

    public async Task<Feedback?> GetByPatientIdAsync(Guid patientId, CancellationToken ct = default)
        => await db.Feedbacks
            .Include(f => f.Patient).ThenInclude(p => p!.User)
            .FirstOrDefaultAsync(f => f.PatientId == patientId, ct);

    public async Task AddAsync(Feedback feedback, CancellationToken ct = default)
    {
        await db.Feedbacks.AddAsync(feedback, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Feedback feedback, CancellationToken ct = default)
    {
        db.Feedbacks.Update(feedback);
        await db.SaveChangesAsync(ct);
    }
}
