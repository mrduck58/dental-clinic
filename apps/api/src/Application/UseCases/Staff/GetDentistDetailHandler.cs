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
    int PatientCount,
    // ── Hồ sơ chuyên môn mở rộng ────────────────────────────────────────────
    string? Gender,
    string? Department,
    string? Position,
    string? LicenseNumber,
    DateOnly? CertificateIssuedDate,
    DateOnly? StartDate,
    string? EmploymentType,
    string? Shift,
    // ── Số liệu hoạt động ───────────────────────────────────────────────────
    int AppointmentCount,
    double AverageRating,
    int ReviewCount,
    IReadOnlyList<string> Services);

public class GetDentistDetailHandler(AppDbContext dbContext)
{
    public async Task<DentistDetailDto?> HandleAsync(Guid dentistId, CancellationToken ct = default)
    {
        var dentist = await dbContext.Dentists
            .AsNoTracking()
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.Id == dentistId || d.UserId == dentistId, ct);
        if (dentist is null) return null;

        // Các buổi khám đã hoàn tất — dùng chung cho số bệnh nhân, số ca và danh sách dịch vụ.
        var completedAppointments = dbContext.Appointments
            .AsNoTracking()
            .Where(a => a.DentistId == dentist.Id &&
                        (a.Status == AppointmentStatus.Completed || a.Status == AppointmentStatus.PendingPayment));

        var patientCount = await completedAppointments
            .Select(a => a.PatientId)
            .Distinct()
            .CountAsync(ct);

        var appointmentCount = await completedAppointments.CountAsync(ct);

        // Dịch vụ bác sĩ đã thực hiện, xếp theo số ca giảm dần (tối đa 8 dịch vụ).
        var services = await completedAppointments
            .Where(a => a.Service != null && a.Service.Name != null)
            .GroupBy(a => a.Service!.Name)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .Take(8)
            .ToListAsync(ct);

        var ratings = await dbContext.DentistReviews
            .AsNoTracking()
            .Where(r => r.DentistId == dentist.Id)
            .Select(r => r.Rating)
            .ToListAsync(ct);

        var averageRating = ratings.Count == 0 ? 0 : Math.Round(ratings.Average(), 1);

        return new DentistDetailDto(
            dentist.Id,
            dentist.FullName,
            dentist.Specialization,
            dentist.ProfilePictureUrl,
            dentist.ExperienceYears,
            dentist.Biography,
            dentist.Education,
            dentist.CertificateIssuedBy,
            patientCount,
            dentist.User?.Gender,
            dentist.Department,
            dentist.Position,
            dentist.LicenseNumber,
            dentist.CertificateIssuedDate,
            dentist.StartDate,
            dentist.EmploymentType,
            dentist.Shift,
            appointmentCount,
            averageRating,
            ratings.Count,
            services);
    }
}
