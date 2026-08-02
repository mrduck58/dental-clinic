using DentalClinic.API.Application.DTOs.Patients;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Patients;

public record CreateFamilyMemberCommand(
    Guid UserId,
    string FullName,
    string Relationship,
    DateOnly? DateOfBirth,
    string Gender,
    string? PhoneNumber,
    string? ProfilePictureUrl) : IRequest<FamilyMemberDto>;

public class CreateFamilyMemberHandler(
    PatientAccessHelper patientAccess,
    IPatientRepository patientRepository,
    IUserRepository userRepository) : IRequestHandler<CreateFamilyMemberCommand, FamilyMemberDto>
{
    public async Task<FamilyMemberDto> Handle(CreateFamilyMemberCommand command, CancellationToken ct)
    {
        var primaryPatient = await patientAccess.GetOrCreatePrimaryPatientAsync(command.UserId, ct)
            ?? throw new NotFoundException("Patient profile not found.");

        // Create a placeholder user for the family member
        var placeholderUser = User.CreateEmployee(
            email: $"family-{Guid.NewGuid()}@songiangdental.com",
            role: "Patient",
            phoneNumber: command.PhoneNumber,
            fullName: command.FullName
        );
        placeholderUser.UpdateGender(command.Gender);
        await userRepository.AddAsync(placeholderUser, ct);

        var member = Patient.Create(
            userId: placeholderUser.Id,
            dateOfBirth: command.DateOfBirth,
            address: null,
            profilePictureUrl: command.ProfilePictureUrl,
            primaryPatientId: primaryPatient.Id,
            relationship: command.Relationship
        );

        await patientRepository.AddAsync(member, ct);

        return new FamilyMemberDto(
            member.Id,
            member.FullName,
            member.Relationship ?? string.Empty,
            member.DateOfBirth,
            member.Gender,
            member.PhoneNumber,
            member.ProfilePictureUrl
        );
    }
}
