using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Patients;

public record GetFamilyMemberMedicalHistoryQuery(Guid UserId, Guid PatientId) : IRequest<string?>;

public class GetFamilyMemberMedicalHistoryHandler(
    PatientAccessHelper patientAccess,
    IPatientRepository patientRepository) : IRequestHandler<GetFamilyMemberMedicalHistoryQuery, string?>
{
    public async Task<string?> Handle(GetFamilyMemberMedicalHistoryQuery query, CancellationToken ct)
    {
        var primaryPatient = await patientAccess.GetOrCreatePrimaryPatientAsync(query.UserId, ct)
            ?? throw new NotFoundException("Patient profile not found.");

        var patient = await patientRepository.GetByIdAsync(query.PatientId, ct)
            ?? throw new NotFoundException("Patient not found.");

        if (!PatientAccessHelper.IsSelfOrFamilyMember(patient, primaryPatient))
            throw new ForbiddenException("Bạn không có quyền xem hồ sơ này.");

        return patient.MedicalHistory;
    }
}
