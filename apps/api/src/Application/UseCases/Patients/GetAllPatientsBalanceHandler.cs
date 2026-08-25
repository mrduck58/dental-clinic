using DentalClinic.API.Application.DTOs.Patients;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Patients;

/// <summary>Công nợ tổng hợp của TẤT CẢ bệnh nhân trong hệ thống — kể cả người chưa từng có liệu trình
/// nào (hiện 0đ đã thu / 0đ còn nợ) — đã thanh toán bao nhiêu, còn nợ bao nhiêu, theo từng dịch vụ.</summary>
public record GetAllPatientsBalanceQuery : IRequest<List<PatientBalanceDto>>;

public class GetAllPatientsBalanceHandler(
    IPatientRepository patientRepository,
    ITreatmentPlanRepository treatmentPlanRepository)
    : IRequestHandler<GetAllPatientsBalanceQuery, List<PatientBalanceDto>>
{
    public async Task<List<PatientBalanceDto>> Handle(GetAllPatientsBalanceQuery query, CancellationToken ct)
    {
        var patients = await patientRepository.GetAllAsync(ct);
        var plans = await treatmentPlanRepository.GetAllWithServiceAsync(ct);
        var planIds = plans.Select(p => p.Id).ToList();
        var paidMap = await treatmentPlanRepository.GetPlanPaidMapAsync(planIds, ct);

        var plansByPatient = plans.ToLookup(p => p.PatientId);

        var result = patients.Select(patient =>
        {
            var patientPlans = plansByPatient[patient.Id];

            var services = patientPlans
                .GroupBy(p => new { p.ServiceId, ServiceName = p.Service.Name })
                .Select(g =>
                {
                    var cost = g.Sum(p => p.TotalCost);
                    var paid = g.Sum(p => paidMap.GetValueOrDefault(p.Id, 0m));
                    return new PatientServiceBalanceDto(
                        g.Key.ServiceId, g.Key.ServiceName, cost, paid, Math.Max(0m, cost - paid));
                })
                .OrderByDescending(s => s.RemainingAmount)
                .ToList();

            var totalCost = services.Sum(s => s.TotalCost);
            var totalPaid = services.Sum(s => s.AmountPaid);
            var planList = patientPlans.ToList();

            return new PatientBalanceDto(
                patient.Id, patient.FullName, patient.PhoneNumber,
                totalCost, totalPaid, Math.Max(0m, totalCost - totalPaid),
                planList.Count,
                planList.Count > 0 ? planList.Max(p => p.CreatedAt) : null,
                services);
        });

        return result.OrderByDescending(p => p.RemainingAmount).ToList();
    }
}
