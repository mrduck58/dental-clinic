using DentalClinic.API.Domain.Exceptions;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Patients;

public record GetMyMedicalHistoryQuery(Guid UserId) : IRequest<string?>;

public class GetMyMedicalHistoryHandler(PatientAccessHelper patientAccess)
    : IRequestHandler<GetMyMedicalHistoryQuery, string?>
{
    public async Task<string?> Handle(GetMyMedicalHistoryQuery query, CancellationToken ct)
    {
        var patient = await patientAccess.GetOrCreatePrimaryPatientAsync(query.UserId, ct)
            ?? throw new NotFoundException("Patient profile not found.");

        return patient.MedicalHistory;
    }
}
