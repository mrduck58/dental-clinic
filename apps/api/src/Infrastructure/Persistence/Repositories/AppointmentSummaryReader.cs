using DentalClinic.API.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Infrastructure.Persistence.Repositories;

public class AppointmentSummaryReader(AppDbContext db) : IAppointmentSummaryReader
{
    public async Task<AppointmentSummary?> GetSummaryAsync(Guid appointmentId, CancellationToken ct = default)
        => await db.Appointments
            .AsNoTracking()
            .Where(a => a.Id == appointmentId)
            .Select(a => new AppointmentSummary(a.PatientId, a.Patient.FullName, a.Dentist.FullName, a.Service == null ? null : a.Service.Name))
            .FirstOrDefaultAsync(ct);
}
