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
        // Client gửi giờ VN local, .toUtc() → VD: 07:30 VN = 00:30 UTC.
        // Lọc theo cửa sổ VN midnight, convert sang UTC (offset=0) để Npgsql chấp nhận.
        var vnOffset = TimeSpan.FromHours(7);
        var startUtc = new DateTimeOffset(date.Year, date.Month, date.Day, 0, 0, 0, vnOffset).ToUniversalTime();
        var endUtc   = startUtc.AddDays(1);
        return await dbContext.Appointments
            .Include(a => a.Service)
            .Where(a => a.AppointmentDate >= startUtc && a.AppointmentDate < endUtc
                     && a.Status != DentalClinic.API.Domain.Enums.AppointmentStatus.Cancelled)
            .ToListAsync(cancellationToken);
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

    public async Task<Appointment?> GetInProgressByDentistAsync(
        Guid dentistId, Guid excludeAppointmentId,
        DateTimeOffset utcStart, DateTimeOffset utcEnd,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Appointments
            .Include(a => a.Patient)
            .FirstOrDefaultAsync(a =>
                a.DentistId == dentistId &&
                a.Id != excludeAppointmentId &&
                a.Status == DentalClinic.API.Domain.Enums.AppointmentStatus.InProgress &&
                a.AppointmentDate >= utcStart &&
                a.AppointmentDate < utcEnd, cancellationToken);
    }
}
