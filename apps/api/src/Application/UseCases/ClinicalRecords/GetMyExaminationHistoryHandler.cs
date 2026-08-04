using DentalClinic.API.Application.DTOs.ClinicalRecords;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Application.UseCases.ClinicalRecords;

/// <summary>
/// GET api/appointments/my/medical-history — trước đây viết THẲNG trong AppointmentsController
/// (truy vấn EF + <c>GetFollowUpChainAsync</c> là private method của controller). Chuyển nguyên
/// logic về handler; đặt tên <c>GetMyExaminationHistory</c> chứ không phải
/// <c>GetMyMedicalHistory</c> để không trùng với <c>UseCases.Patients.GetMyMedicalHistoryQuery</c>
/// (tiền sử bệnh lý khai báo trong hồ sơ — nghiệp vụ khác hẳn).
/// </summary>
public record GetMyExaminationHistoryQuery(Guid UserId, Guid? PatientId) : IRequest<List<MyMedicalHistoryDto>>;

public class GetMyExaminationHistoryHandler(AppDbContext dbContext)
    : IRequestHandler<GetMyExaminationHistoryQuery, List<MyMedicalHistoryDto>>
{
    public async Task<List<MyMedicalHistoryDto>> Handle(GetMyExaminationHistoryQuery request, CancellationToken ct)
    {
        var userId = request.UserId;
        var patientId = request.PatientId;

        var patient = await dbContext.Patients.FirstOrDefaultAsync(p => p.UserId == userId, ct);
        if (patient is null) return new List<MyMedicalHistoryDto>();

        var appointments = await dbContext.Appointments
            .Include(a => a.Dentist).ThenInclude(d => d.User)
            .Include(a => a.Patient)
            .Include(a => a.Service)
            .Include(a => a.Diagnoses)
            .Include(a => a.TreatmentPlans).ThenInclude(tp => tp.Service)
            .Include(a => a.Prescriptions).ThenInclude(p => p.Items)
            .Where(a => (a.PatientId == patient.Id || a.Patient.PrimaryPatientId == patient.Id) &&
                        (patientId == null || a.PatientId == patientId) &&
                        (a.Status == AppointmentStatus.Completed || a.Status == AppointmentStatus.PendingPayment))
            .OrderByDescending(a => a.AppointmentDate)
            .Take(50)
            .ToListAsync(ct);

        // Chuỗi buổi hẹn gốc của lượt tái khám (đi ngược FollowUpFromAppointmentId) — để mobile gộp
        // liệu trình điều trị dài hạn xuyên suốt nhiều buổi tái khám thay vì chỉ khớp đúng 1 buổi.
        var chainByAppointment = new Dictionary<Guid, List<Guid>>();
        foreach (var a in appointments)
        {
            chainByAppointment[a.Id] = await ClinicalRecordMappers.GetFollowUpChainAsync(dbContext, a.Id, ct);
        }

        return appointments.Select(a => new MyMedicalHistoryDto(
            a.Id,
            ClinicalRecordMappers.AppointmentCode(a),
            a.AppointmentDate,
            a.DentistId,
            a.Dentist.FullName,
            a.Service?.Name ?? "Khám tổng quát",
            a.Symptoms,
            a.PatientId,
            a.Patient.FullName,
            a.PatientId == patient.Id ? "Tôi" : (a.Patient.Relationship ?? string.Empty),
            a.FollowUpDate,
            a.FollowUpNote,
            chainByAppointment[a.Id],
            ClinicalRecordMappers.ToMedicalHistoryDiagnoses(a),
            ClinicalRecordMappers.ToMedicalHistoryTreatmentPlans(a),
            ClinicalRecordMappers.ToMedicalHistoryPrescriptionItems(a)
        )).ToList();
    }
}
