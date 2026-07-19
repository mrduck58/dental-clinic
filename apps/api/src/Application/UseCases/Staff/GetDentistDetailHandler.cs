using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Application.UseCases.Staff;

public record DentistDetailDto(
    Guid Id,
    string FullName,
    string? Specialty,
    string? ProfilePictureUrl,
    int? YearsOfExperience,
    string? Bio,
    string? Education,
    string? CertificateIssuedBy,
    int PatientCount);

public class GetDentistDetailHandler(AppDbContext dbContext)
{
    public async Task<DentistDetailDto?> HandleAsync(Guid dentistId, CancellationToken ct = default)
    {
        var dentist = await dbContext.Dentists
            .AsNoTracking()
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.Id == dentistId, ct);
        if (dentist is null) return null;

        var patientCount = await dbContext.Appointments
            .Where(a => a.DentistId == dentistId &&
                        (a.Status == AppointmentStatus.Completed || a.Status == AppointmentStatus.PendingPayment))
            .Select(a => a.PatientId)
            .Distinct()
            .CountAsync(ct);

        return new DentistDetailDto(
            dentist.Id,
            dentist.FullName,
            dentist.Specialization,
            dentist.ProfilePictureUrl,
            dentist.ExperienceYears,
            dentist.Biography,
            dentist.Education,
            dentist.CertificateIssuedBy,
            patientCount);
    }
}
