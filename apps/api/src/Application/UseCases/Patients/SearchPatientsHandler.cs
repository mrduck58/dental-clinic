using DentalClinic.API.Application.DTOs.Patients;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Patients;

public record SearchPatientsQuery(string? Q, int Limit) : IRequest<IEnumerable<PatientSearchResultDto>>;

public class SearchPatientsHandler(IPatientRepository patientRepository)
    : IRequestHandler<SearchPatientsQuery, IEnumerable<PatientSearchResultDto>>
{
    public async Task<IEnumerable<PatientSearchResultDto>> Handle(SearchPatientsQuery query, CancellationToken ct)
    {
        var term = (query.Q ?? string.Empty).Trim();
        if (term.Length < 2) return [];

        var take = query.Limit is > 0 and <= 20 ? query.Limit : 8;
        var patients = await patientRepository.SearchAsync(term, take, ct);

        return patients.Select(p => new PatientSearchResultDto(
            p.Id,
            p.FullName,
            p.PhoneNumber ?? p.User?.PhoneNumber,
            p.DateOfBirth,
            p.Gender,
            p.User != null && p.User.PasswordHash != null
        ));
    }
}
