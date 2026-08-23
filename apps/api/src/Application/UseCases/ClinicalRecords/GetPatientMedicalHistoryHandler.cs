using DentalClinic.API.Application.DTOs.ClinicalRecords;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.ClinicalRecords;

/// <summary>
/// GET api/appointments/patients/{patientId}/medical-history — trước đây viết THẲNG trong
/// AppointmentsController bằng truy vấn EF. Chuyển nguyên logic về handler.
/// </summary>
public record GetPatientMedicalHistoryQuery(Guid PatientId) : IRequest<List<PatientMedicalHistoryDto>>;

public class GetPatientMedicalHistoryHandler(IAppointmentRepository appointmentRepository)
    : IRequestHandler<GetPatientMedicalHistoryQuery, List<PatientMedicalHistoryDto>>
{
    public async Task<List<PatientMedicalHistoryDto>> Handle(GetPatientMedicalHistoryQuery request, CancellationToken ct)
    {
        var patientId = request.PatientId;

        var appointments = await appointmentRepository.GetCompletedHistoryByPatientAsync(patientId, 50, ct);

        return appointments.Select(a => new PatientMedicalHistoryDto(
            a.Id,
            ClinicalRecordMappers.AppointmentCode(a),
            a.AppointmentDate,
            a.Dentist.FullName,
            a.Service?.Name ?? "Khám tổng quát",
            a.Symptoms,
            ClinicalRecordMappers.ToMedicalHistoryDiagnoses(a),
            ClinicalRecordMappers.ToMedicalHistoryTreatmentPlans(a),
            ClinicalRecordMappers.ToMedicalHistoryPrescriptionItems(a),
            ClinicalRecordMappers.ToMedicalHistoryPhotos(a)
        )).ToList();
    }
}
