using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Infrastructure.Persistence.Repositories;

public class AppointmentChangeRequestRepository(AppDbContext dbContext) : IAppointmentChangeRequestRepository
{
    public async Task<AppointmentChangeRequest?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await dbContext.AppointmentChangeRequests
            .Include(r => r.Appointment).ThenInclude(a => a.Patient).ThenInclude(p => p.User)
            .Include(r => r.Appointment).ThenInclude(a => a.Dentist).ThenInclude(d => d.Employee).ThenInclude(e => e.User)
            .Include(r => r.Appointment).ThenInclude(a => a.Service)
            .Include(r => r.DesiredDentist).ThenInclude(d => d.Employee).ThenInclude(e => e.User)
            .Include(r => r.RequestedByUser)
            .Include(r => r.ProcessedByUser)
            .FirstOrDefaultAsync(r => r.Id == id, ct);
    }

    public async Task<AppointmentChangeRequest?> GetPendingByAppointmentIdAsync(Guid appointmentId, CancellationToken ct = default)
    {
        return await dbContext.AppointmentChangeRequests
            .Include(r => r.DesiredDentist).ThenInclude(d => d.Employee).ThenInclude(e => e.User)
            .FirstOrDefaultAsync(r => r.AppointmentId == appointmentId && r.Status == AppointmentChangeRequestStatus.Pending, ct);
    }

    public async Task<IReadOnlyList<AppointmentChangeRequest>> GetByAppointmentIdAsync(Guid appointmentId, CancellationToken ct = default)
    {
        return await dbContext.AppointmentChangeRequests
            .Where(r => r.AppointmentId == appointmentId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<AppointmentChangeRequest>> GetStaffChangeRequestsAsync(
        AppointmentChangeRequestStatus? status = null,
        DateOnly? date = null,
        CancellationToken ct = default)
    {
        var query = dbContext.AppointmentChangeRequests
            .Include(r => r.Appointment).ThenInclude(a => a.Patient).ThenInclude(p => p.User)
            .Include(r => r.Appointment).ThenInclude(a => a.Dentist).ThenInclude(d => d.Employee).ThenInclude(e => e.User)
            .Include(r => r.Appointment).ThenInclude(a => a.Service)
            .Include(r => r.DesiredDentist).ThenInclude(d => d.Employee).ThenInclude(e => e.User)
            .Include(r => r.RequestedByUser)
            .Include(r => r.ProcessedByUser)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        if (date.HasValue)
        {
            var vnOffset = TimeSpan.FromHours(7);
            var start = new DateTimeOffset(date.Value.Year, date.Value.Month, date.Value.Day, 0, 0, 0, vnOffset).ToUniversalTime();
            var end = start.AddDays(1);
            query = query.Where(r => r.Appointment.AppointmentDate >= start && r.Appointment.AppointmentDate < end);
        }

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task AddAsync(AppointmentChangeRequest request, CancellationToken ct = default)
    {
        await dbContext.AppointmentChangeRequests.AddAsync(request, ct);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(AppointmentChangeRequest request, CancellationToken ct = default)
    {
        dbContext.AppointmentChangeRequests.Update(request);
        await dbContext.SaveChangesAsync(ct);
    }
}
