using DentalClinic.API.Application.DTOs.ClinicalRecords;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Exceptions;

namespace DentalClinic.API.Application.UseCases.ClinicalRecords;

public class TreatmentPlanQueryHelper(
    ITreatmentPlanRepository treatmentPlanRepository,
    IAppointmentRepository appointmentRepository,
    ITreatmentProcedureRepository treatmentProcedureRepository)
{
    public async Task<List<int>> GetProcedureStepNumbersAsync(Guid serviceId, CancellationToken ct) =>
        (await treatmentProcedureRepository.GetByServiceIdAsync(serviceId, ct)).Select(p => p.StepNumber).ToList();

    public async Task<Dictionary<Guid, List<int>>> GetProcedureStepNumbersMapAsync(List<Guid> serviceIds, CancellationToken ct) =>
        (await treatmentProcedureRepository.GetByServiceIdsAsync(serviceIds.Distinct().ToList(), ct))
            .GroupBy(p => p.ServiceId)
            .ToDictionary(g => g.Key, g => g.Select(p => p.StepNumber).ToList());

    public async Task SyncStatusWithProgressAsync(TreatmentPlanItem item, CancellationToken ct)
    {
        if (item.Status == TreatmentPlanItemStatus.Cancelled) return;

        var procStepNumbers = await GetProcedureStepNumbersAsync(item.ServiceId, ct);
        var distinctSessions = item.Sessions.DistinctBy(s => s.Id).ToList();
        var total = procStepNumbers.Count > 0 ? procStepNumbers.Count : distinctSessions.Count;
        var completed = distinctSessions.Count(s => s.Percent >= 100 || s.Status == TreatmentSessionStatus.Completed);
        var inProgress = distinctSessions.Any(s => s.Percent > 0 || s.Status == TreatmentSessionStatus.InProgress || s.Status == TreatmentSessionStatus.Scheduled);

        var status = total == 0
            ? TreatmentPlanItemStatus.Planned
            : (completed >= total && total > 0)
                ? TreatmentPlanItemStatus.Completed
                : (inProgress || completed > 0 || distinctSessions.Count > 0)
                    ? TreatmentPlanItemStatus.InProgress
                    : TreatmentPlanItemStatus.Planned;

        if (item.Status != status)
        {
            item.SetStatus(status);
        }

        if (item.TreatmentPlan != null && item.TreatmentPlan.Status != TreatmentPlanStatus.Cancelled)
        {
            item.TreatmentPlan.SetStatus(Enum.Parse<TreatmentPlanStatus>(item.Status.ToString()));
        }
    }

    public Task<Dictionary<Guid, decimal>> GetAmountPaidMapAsync(List<Guid> planIds, CancellationToken ct) =>
        treatmentPlanRepository.GetPlanPaidMapAsync(planIds, ct);

    public async Task<decimal> GetAmountPaidAsync(Guid treatmentPlanId, CancellationToken ct) =>
        (await GetAmountPaidMapAsync(new List<Guid> { treatmentPlanId }, ct))
            .GetValueOrDefault(treatmentPlanId, 0m);

    public async Task<HashSet<Guid>> GetInvoicedPlanIdsAsync(List<Guid> planIds, CancellationToken ct) =>
        (await treatmentPlanRepository.GetPlanBilledMapAsync(planIds, ct)).Keys.ToHashSet();

    public async Task<bool> IsInvoicedAsync(Guid treatmentPlanId, CancellationToken ct) =>
        (await GetInvoicedPlanIdsAsync(new List<Guid> { treatmentPlanId }, ct)).Count > 0;

    public async Task<TreatmentPlanDto> LoadDtoAsync(Guid planId, CancellationToken ct)
    {
        var plan = await treatmentPlanRepository.GetByIdWithDetailsAsync(planId, ct)
            ?? throw new NotFoundException("Không tìm thấy liệu trình điều trị.");

        var planIds = new List<Guid> { planId };
        var paid = (await GetAmountPaidMapAsync(planIds, ct)).GetValueOrDefault(planId, 0m);
        var isInvoiced = (await GetInvoicedPlanIdsAsync(planIds, ct)).Contains(planId);

        var serviceIds = plan.Items.Select(i => i.ServiceId).ToList();
        var procMap = await GetProcedureStepNumbersMapAsync(serviceIds, ct);
        var procSteps = plan.Items.FirstOrDefault() != null ? procMap.GetValueOrDefault(plan.Items.First().ServiceId) : null;

        return ClinicalRecordMappers.ToDto(plan, paid, isInvoiced, procSteps);
    }

    public Task<bool> HasActiveVisitAsync(Guid patientId, CancellationToken ct) =>
        appointmentRepository.HasActiveVisitAsync(patientId, ct);
}
