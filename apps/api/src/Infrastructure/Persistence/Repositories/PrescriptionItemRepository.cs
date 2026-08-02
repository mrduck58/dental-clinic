using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Infrastructure.Persistence.Repositories;

public class PrescriptionItemRepository(AppDbContext db) : IPrescriptionItemRepository
{
    public async Task<PrescriptionItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.PrescriptionItems.FirstOrDefaultAsync(i => i.Id == id, ct);

    public async Task AddAsync(PrescriptionItem item, CancellationToken ct = default)
    {
        await db.PrescriptionItems.AddAsync(item, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(PrescriptionItem item, CancellationToken ct = default)
    {
        db.PrescriptionItems.Update(item);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(PrescriptionItem item, CancellationToken ct = default)
    {
        db.PrescriptionItems.Remove(item);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<PrescriptionItem>> GetActiveMedicationRemindersByPatientAsync(
        Guid patientId, CancellationToken ct = default)
        => await db.PrescriptionItems
            .AsNoTracking()
            .Include(i => i.Prescription).ThenInclude(p => p.Appointment).ThenInclude(a => a.Patient)
            .Where(i => i.TimesPerDay != null && i.TimesPerDay > 0 && i.DurationDays != null && i.StartDate != null &&
                        (i.Prescription.Appointment.PatientId == patientId ||
                         i.Prescription.Appointment.Patient.PrimaryPatientId == patientId))
            .ToListAsync(ct);
}
