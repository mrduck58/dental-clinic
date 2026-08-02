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
    int PatientCount);

public record GetDentistDetailQuery(Guid DentistId) : IRequest<DentistDetailDto?>;

public class GetDentistDetailHandler(
    IDentistRepository dentistRepository,
    IAppointmentRepository appointmentRepository)
    : IRequestHandler<GetDentistDetailQuery, DentistDetailDto?>
{
    public async Task<DentistDetailDto?> Handle(GetDentistDetailQuery request, CancellationToken ct)
    {
        var dentist = await dentistRepository.GetByIdOrUserIdAsync(request.DentistId, ct);
        if (dentist is null) return null;

        var patientCount = await appointmentRepository.CountDistinctPatientsWithCompletedVisitAsync(dentist.Id, ct);

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
