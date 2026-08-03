using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Patients;

public record DeleteFamilyMemberCommand(Guid UserId, Guid Id) : IRequest;

public class DeleteFamilyMemberHandler(
    PatientAccessHelper patientAccess,
    IPatientRepository patientRepository) : IRequestHandler<DeleteFamilyMemberCommand>
{
    public async Task Handle(DeleteFamilyMemberCommand command, CancellationToken ct)
    {
        var primaryPatient = await patientAccess.GetOrCreatePrimaryPatientAsync(command.UserId, ct)
            ?? throw new NotFoundException("Patient profile not found.");

        var member = await patientRepository.GetByIdAsync(command.Id, ct);
        if (member == null || member.PrimaryPatientId != primaryPatient.Id)
            throw new NotFoundException("Family member not found.");

        await patientRepository.DeleteAsync(member, ct);
    }
}
