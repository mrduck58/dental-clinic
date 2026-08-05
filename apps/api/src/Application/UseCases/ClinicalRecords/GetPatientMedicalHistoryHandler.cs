using DentalClinic.API.Application.DTOs.ClinicalRecords;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Application.UseCases.ClinicalRecords;

/// <summary>
/// GET api/appointments/patients/{patientId}/medical-history — trước đây viết THẲNG trong
/// AppointmentsController bằng truy vấn EF. Chuyển nguyên logic về handler.
/// </summary>
public record GetPatientMedicalHistoryQuery(Guid PatientId) : IRequest<List<PatientMedicalHistoryDto>>;

public class GetPatientMedicalHistoryHandler(AppDbContext dbContext)
    : IRequestHandler<GetPatientMedicalHistoryQuery, List<PatientMedicalHistoryDto>>
{
    public async Task<List<PatientMedicalHistoryDto>> Handle(GetPatientMedicalHistoryQuery request, CancellationToken ct)
    {
        var patientId = request.PatientId;

        var appointments = await dbContext.Appointments
            .Include(a => a.Dentist).ThenInclude(d => d.Employee).ThenInclude(e => e.User)
            .Include(a => a.Service)
            .Include(a => a.Diagnoses)
            .Include(a => a.TreatmentPlans).ThenInclude(tp => tp.Service)
            .Include(a => a.Prescriptions).ThenInclude(p => p.Items)
            .Where(a => a.PatientId == patientId &&
                        (a.Status == AppointmentStatus.Completed || a.Status == AppointmentStatus.PendingPayment))
            .OrderByDescending(a => a.AppointmentDate)
            .Take(50)
            .ToListAsync(ct);

        return appointments.Select(a => new PatientMedicalHistoryDto(
            a.Id,
            ClinicalRecordMappers.AppointmentCode(a),
            a.AppointmentDate,
            a.Dentist.FullName,
            a.Service?.Name ?? "Khám tổng quát",
            a.Symptoms,
            ClinicalRecordMappers.ToMedicalHistoryDiagnoses(a),
            ClinicalRecordMappers.ToMedicalHistoryTreatmentPlans(a),
            ClinicalRecordMappers.ToMedicalHistoryPrescriptionItems(a)
        )).ToList();
    }
}
