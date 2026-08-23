using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Infrastructure.Persistence.Repositories;

public class AppointmentPhotoRepository(AppDbContext db) : IAppointmentPhotoRepository
{
    public async Task<AppointmentPhoto?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.AppointmentPhotos.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<List<AppointmentPhoto>> GetByAppointmentIdAsync(Guid appointmentId, string? section = null, CancellationToken ct = default)
        => await db.AppointmentPhotos
            .AsNoTracking()
            .Where(p => p.AppointmentId == appointmentId && (section == null || p.Section == section))
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

    public async Task AddAsync(AppointmentPhoto photo, CancellationToken ct = default)
    {
        await db.AppointmentPhotos.AddAsync(photo, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(AppointmentPhoto photo, CancellationToken ct = default)
    {
        db.AppointmentPhotos.Update(photo);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(AppointmentPhoto photo, CancellationToken ct = default)
    {
        db.AppointmentPhotos.Remove(photo);
        await db.SaveChangesAsync(ct);
    }
}
