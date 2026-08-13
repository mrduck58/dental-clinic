using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Infrastructure.Persistence.Repositories;

public class PrescriptionRepository(AppDbContext db) : IPrescriptionRepository
{
    public async Task<Prescription?> GetByIdWithItemsAsync(Guid id, CancellationToken ct = default)
        => await db.Prescriptions
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<Prescription?> GetByAppointmentIdWithItemsAsync(Guid appointmentId, CancellationToken ct = default)
        => await db.Prescriptions
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.AppointmentId == appointmentId, ct);

    public async Task AddAsync(Prescription prescription, CancellationToken ct = default)
    {
        await db.Prescriptions.AddAsync(prescription, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Prescription prescription, CancellationToken ct = default)
    {
        db.Prescriptions.Update(prescription);
        await db.SaveChangesAsync(ct);
    }
}
