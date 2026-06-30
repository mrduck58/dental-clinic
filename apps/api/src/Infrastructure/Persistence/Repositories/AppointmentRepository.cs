using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Infrastructure.Persistence.Repositories;

public class AppointmentRepository(AppDbContext dbContext) : IAppointmentRepository
{
    public async Task<Appointment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Appointments
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Appointment>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Appointments
            .Where(a => a.PatientId == patientId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Appointment>> GetByDentistIdAsync(Guid dentistId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Appointments
            .Where(a => a.DentistId == dentistId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Appointment>> GetByDateAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var start = new DateTimeOffset(date.Year, date.Month, date.Day, 0, 0, 0, TimeSpan.Zero);
        var end = start.AddDays(1);
        return await dbContext.Appointments
            .Where(a => a.AppointmentDate >= start && a.AppointmentDate < end)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsSlotBookedAsync(Guid dentistId, DateTimeOffset slotTime, CancellationToken cancellationToken = default)
    {
        return await dbContext.Appointments
            .AnyAsync(a =>
                a.DentistId == dentistId &&
                a.AppointmentDate == slotTime &&
                a.Status != DentalClinic.API.Domain.Enums.AppointmentStatus.Cancelled,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Appointment>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Appointments
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Appointment appointment, CancellationToken cancellationToken = default)
    {
        await dbContext.Appointments.AddAsync(appointment, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Appointment appointment, CancellationToken cancellationToken = default)
    {
        dbContext.Appointments.Update(appointment);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid?> GetDentistUserIdAsync(Guid dentistId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<Dentist>()
            .Where(d => d.Id == dentistId)
            .Select(d => (Guid?)d.UserId)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
