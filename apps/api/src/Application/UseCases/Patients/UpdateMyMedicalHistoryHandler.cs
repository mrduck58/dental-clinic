using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Patients;

public record UpdateMyMedicalHistoryCommand(Guid UserId, string? MedicalHistory) : IRequest;

public class UpdateMyMedicalHistoryHandler(
    PatientAccessHelper patientAccess,
    IPatientRepository patientRepository) : IRequestHandler<UpdateMyMedicalHistoryCommand>
{
    public async Task Handle(UpdateMyMedicalHistoryCommand command, CancellationToken ct)
    {
        var patient = await patientAccess.GetOrCreatePrimaryPatientAsync(command.UserId, ct)
            ?? throw new NotFoundException("Patient profile not found.");

        patient.UpdateMedicalHistory(command.MedicalHistory ?? string.Empty);
        await patientRepository.UpdateAsync(patient, ct);
    }
}
