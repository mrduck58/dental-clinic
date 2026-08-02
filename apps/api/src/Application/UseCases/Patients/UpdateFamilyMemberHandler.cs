using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Patients;

public record UpdateFamilyMemberCommand(
    Guid UserId,
    Guid Id,
    string FullName,
    string Relationship,
    DateOnly? DateOfBirth,
    string Gender,
    string? PhoneNumber,
    string? ProfilePictureUrl) : IRequest;

public class UpdateFamilyMemberHandler(
    PatientAccessHelper patientAccess,
    IPatientRepository patientRepository) : IRequestHandler<UpdateFamilyMemberCommand>
{
    public async Task Handle(UpdateFamilyMemberCommand command, CancellationToken ct)
    {
        var primaryPatient = await patientAccess.GetOrCreatePrimaryPatientAsync(command.UserId, ct)
            ?? throw new NotFoundException("Patient profile not found.");

        var member = await patientRepository.GetByIdAsync(command.Id, ct);
        if (member == null || member.PrimaryPatientId != primaryPatient.Id)
            throw new NotFoundException("Family member not found.");

        member.SetFullName(command.FullName);
        member.SetDateOfBirth(command.DateOfBirth);
        member.SetGender(command.Gender);
        member.SetPhoneNumber(command.PhoneNumber);
        member.UpdateFamilyRelation(primaryPatient.Id, command.Relationship);
        member.UpdateProfilePicture(command.ProfilePictureUrl);

        await patientRepository.UpdateAsync(member, ct);
    }
}
