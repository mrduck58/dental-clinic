using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Patients;

public record UpdateFamilyMemberMedicalHistoryCommand(Guid UserId, Guid PatientId, string? MedicalHistory) : IRequest;

public class UpdateFamilyMemberMedicalHistoryHandler(
    PatientAccessHelper patientAccess,
    IPatientRepository patientRepository) : IRequestHandler<UpdateFamilyMemberMedicalHistoryCommand>
{
    public async Task Handle(UpdateFamilyMemberMedicalHistoryCommand command, CancellationToken ct)
    {
        var primaryPatient = await patientAccess.GetOrCreatePrimaryPatientAsync(command.UserId, ct)
            ?? throw new NotFoundException("Patient profile not found.");

        var patient = await patientRepository.GetByIdAsync(command.PatientId, ct)
            ?? throw new NotFoundException("Patient not found.");

        if (!PatientAccessHelper.IsSelfOrFamilyMember(patient, primaryPatient))
            throw new ForbiddenException("Bạn không có quyền sửa hồ sơ này.");

        patient.UpdateMedicalHistory(command.MedicalHistory ?? string.Empty);
        await patientRepository.UpdateAsync(patient, ct);
    }
}
