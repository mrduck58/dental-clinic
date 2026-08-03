using DentalClinic.API.Application.DTOs.Patients;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Patients;

public record GetFamilyMembersQuery(Guid UserId) : IRequest<IEnumerable<FamilyMemberDto>>;

public class GetFamilyMembersHandler(
    PatientAccessHelper patientAccess,
    IPatientRepository patientRepository) : IRequestHandler<GetFamilyMembersQuery, IEnumerable<FamilyMemberDto>>
{
    public async Task<IEnumerable<FamilyMemberDto>> Handle(GetFamilyMembersQuery query, CancellationToken ct)
    {
        var primaryPatient = await patientAccess.GetOrCreatePrimaryPatientAsync(query.UserId, ct)
            ?? throw new NotFoundException("Patient profile not found.");

        var members = await patientRepository.GetFamilyMembersAsync(primaryPatient.Id, ct);

        return members.Select(m => new FamilyMemberDto(
            m.Id,
            m.FullName,
            m.Relationship ?? string.Empty,
            m.DateOfBirth,
            m.Gender,
            m.PhoneNumber,
            m.ProfilePictureUrl
        ));
    }
}
