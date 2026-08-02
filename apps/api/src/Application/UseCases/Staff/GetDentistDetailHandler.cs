using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

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

public record GetDentistDetailQuery(Guid DentistId) : IRequest<DentistDetailDto?>;

public class GetDentistDetailHandler(
    IDentistRepository dentistRepository,
    IAppointmentRepository appointmentRepository,
    IDentistReviewRepository dentistReviewRepository)
    : IRequestHandler<GetDentistDetailQuery, DentistDetailDto?>
{
    /// <summary>Số dịch vụ tiêu biểu hiển thị trên trang chi tiết nha sĩ.</summary>
    private const int TopServiceCount = 8;

    public async Task<DentistDetailDto?> Handle(GetDentistDetailQuery request, CancellationToken ct)
    {
        var dentist = await dentistRepository.GetByIdOrUserIdAsync(request.DentistId, ct);
        if (dentist is null) return null;

        var patientCount = await appointmentRepository.CountDistinctPatientsWithCompletedVisitAsync(dentist.Id, ct);
        var appointmentCount = await appointmentRepository.CountCompletedVisitsAsync(dentist.Id, ct);

        // Dịch vụ bác sĩ đã thực hiện, xếp theo số ca giảm dần.
        var services = await appointmentRepository.GetTopServiceNamesByDentistAsync(dentist.Id, TopServiceCount, ct);

        var ratings = await dentistReviewRepository.GetRatingsByDentistIdAsync(dentist.Id, ct);
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
