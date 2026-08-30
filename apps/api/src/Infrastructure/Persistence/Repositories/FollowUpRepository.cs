namespace DentalClinic.API.Infrastructure.Persistence.Repositories;

using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

public class FollowUpRepository(AppDbContext db) : IFollowUpRepository
{
    public async Task<FollowUp?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.FollowUps.FirstOrDefaultAsync(f => f.Id == id, ct);

    public async Task<FollowUp?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default)
        => await db.FollowUps
            .AsNoTracking()
            .Include(f => f.Patient).ThenInclude(p => p.User)
            .Include(f => f.Dentist).ThenInclude(d => d.Employee).ThenInclude(e => e.User)
            .Include(f => f.TreatmentPlanItem).ThenInclude(tpi => tpi!.Service)
            .Include(f => f.TreatmentSession)
            .Include(f => f.OriginAppointment)
            .Include(f => f.Appointment)
            .FirstOrDefaultAsync(f => f.Id == id, ct);

    public async Task<IReadOnlyList<FollowUp>> GetByPatientIdAsync(Guid patientId, CancellationToken ct = default)
        => await db.FollowUps
            .AsNoTracking()
            .Include(f => f.Dentist).ThenInclude(d => d.Employee).ThenInclude(e => e.User)
            .Include(f => f.TreatmentPlanItem).ThenInclude(tpi => tpi!.Service)
            .Include(f => f.TreatmentSession)
            .Where(f => f.PatientId == patientId)
            .OrderByDescending(f => f.DueDate)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<FollowUp>> GetDueFollowUpsAsync(DateOnly? toDate = null, CancellationToken ct = default)
    {
        var targetDate = toDate ?? DateOnly.FromDateTime(DateTime.Today.AddDays(7));
        return await db.FollowUps
            .AsNoTracking()
            .Include(f => f.Patient).ThenInclude(p => p.User)
            .Include(f => f.Dentist).ThenInclude(d => d.Employee).ThenInclude(e => e.User)
            .Include(f => f.TreatmentPlanItem).ThenInclude(tpi => tpi!.Service)
            .Include(f => f.TreatmentSession)
            .Include(f => f.OriginAppointment)
            .Where(f => f.Status == FollowUpStatus.Pending && f.DueDate <= targetDate)
            .OrderBy(f => f.DueDate)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<FollowUp>> GetPendingByPatientIdAsync(Guid patientId, CancellationToken ct = default)
        => await db.FollowUps
            .AsNoTracking()
            .Include(f => f.Dentist).ThenInclude(d => d.Employee).ThenInclude(e => e.User)
            .Include(f => f.TreatmentPlanItem).ThenInclude(tpi => tpi!.Service)
            .Include(f => f.TreatmentSession)
            .Where(f => f.PatientId == patientId && f.Status == FollowUpStatus.Pending)
            .OrderBy(f => f.DueDate)
            .ToListAsync(ct);

    public async Task AddAsync(FollowUp followUp, CancellationToken ct = default)
    {
        await db.FollowUps.AddAsync(followUp, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(FollowUp followUp, CancellationToken ct = default)
    {
        db.FollowUps.Update(followUp);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(FollowUp followUp, CancellationToken ct = default)
    {
        db.FollowUps.Remove(followUp);
        await db.SaveChangesAsync(ct);
    }
}
