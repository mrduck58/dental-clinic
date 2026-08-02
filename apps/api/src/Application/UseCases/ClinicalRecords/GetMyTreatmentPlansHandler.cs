using DentalClinic.API.Application.DTOs.ClinicalRecords;
using DentalClinic.API.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Application.UseCases.ClinicalRecords;

/// <summary>
/// GET api/appointments/my/treatment-plans — liệu trình điều trị (kèm nhật ký tiến độ thật) của
/// chính bệnh nhân đang đăng nhập và các thành viên gia đình.
/// <para>
/// Trước đây phần dò hồ sơ gia đình viết THẲNG trong AppointmentsController bằng truy vấn EF rồi
/// gọi vòng lặp <c>treatmentPlanHandler.GetByPatientAsync</c>. Chuyển nguyên logic về handler để
/// controller không còn chạm <c>AppDbContext</c>.
/// </para>
/// </summary>
public record GetMyTreatmentPlansQuery(Guid UserId, Guid? PatientId) : IRequest<List<TreatmentPlanDto>>;

public class GetMyTreatmentPlansHandler(AppDbContext dbContext, ISender sender)
    : IRequestHandler<GetMyTreatmentPlansQuery, List<TreatmentPlanDto>>
{
    public async Task<List<TreatmentPlanDto>> Handle(GetMyTreatmentPlansQuery request, CancellationToken ct)
    {
        var patient = await dbContext.Patients.FirstOrDefaultAsync(p => p.UserId == request.UserId, ct);
        if (patient is null) return new List<TreatmentPlanDto>();

        var allowedIds = await dbContext.Patients
            .Where(p => p.Id == patient.Id || p.PrimaryPatientId == patient.Id)
            .Select(p => p.Id)
            .ToListAsync(ct);

        if (request.PatientId != null && !allowedIds.Contains(request.PatientId.Value))
            return new List<TreatmentPlanDto>();

        var targetIds = request.PatientId != null ? new List<Guid> { request.PatientId.Value } : allowedIds;

        var result = new List<TreatmentPlanDto>();
        foreach (var pid in targetIds)
        {
            result.AddRange(await sender.Send(new GetPatientTreatmentPlansQuery(pid), ct));
        }

        return result;
    }
}
