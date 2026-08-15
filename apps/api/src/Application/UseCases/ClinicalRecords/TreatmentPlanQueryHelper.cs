using DentalClinic.API.Application.DTOs.ClinicalRecords;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Exceptions;

namespace DentalClinic.API.Application.UseCases.ClinicalRecords;

/// <summary>
/// Truy vấn công nợ/hóa đơn của liệu trình điều trị, dùng chung giữa các handler TreatmentPlan
/// sau khi god-handler <c>TreatmentPlanHandler</c> (8 method) được tách. Phần map công nợ ủy quyền
/// cho <see cref="ITreatmentPlanRepository"/> — cùng logic dùng bởi <c>InvoiceQueryHelper</c> bên Invoices,
/// tránh lặp lại 2 nơi.
/// </summary>
public class TreatmentPlanQueryHelper(
    ITreatmentPlanRepository treatmentPlanRepository,
    IAppointmentRepository appointmentRepository,
    ITreatmentProcedureRepository treatmentProcedureRepository)
{
    /// <summary>Số hiệu các bước trong quy trình chuẩn của một dịch vụ.</summary>
    public async Task<List<int>> GetProcedureStepNumbersAsync(Guid serviceId, CancellationToken ct) =>
        (await treatmentProcedureRepository.GetByServiceIdAsync(serviceId, ct)).Select(p => p.StepNumber).ToList();

    /// <summary>Quy trình chuẩn của nhiều dịch vụ, gom theo ServiceId — dùng khi map cả danh sách liệu trình.</summary>
    public async Task<Dictionary<Guid, List<int>>> GetProcedureStepNumbersMapAsync(List<Guid> serviceIds, CancellationToken ct) =>
        (await treatmentProcedureRepository.GetByServiceIdsAsync(serviceIds.Distinct().ToList(), ct))
            .GroupBy(p => p.ServiceId)
            .ToDictionary(g => g.Key, g => g.Select(p => p.StepNumber).ToList());

    /// <summary>
    /// Đồng bộ trạng thái liệu trình theo TIẾN ĐỘ CHUYÊN MÔN (không theo thanh toán):
    /// chưa ghi nhận bước nào → Chờ thực hiện, có bước dở dang → Đang thực hiện,
    /// mọi bước quy trình đạt 100% → Hoàn thành.
    /// Liệu trình đã hủy giữ nguyên — hủy là quyết định hành chính, không phải tiến độ.
    /// </summary>
    public async Task SyncStatusWithProgressAsync(TreatmentPlan plan, List<StepProgressEntryDto> entries, CancellationToken ct)
    {
        if (plan.Status == TreatmentPlanStatus.Cancelled) return;

        var stepNumbers = await GetProcedureStepNumbersAsync(plan.ServiceId, ct);
        var progress = ClinicalRecordMappers.CalcStepProgress(entries, stepNumbers);

        var status = entries.Count == 0
            ? TreatmentPlanStatus.Planned
            : progress.AllDone
                ? TreatmentPlanStatus.Completed
                : TreatmentPlanStatus.InProgress;

        if (plan.Status != status) plan.SetStatus(status);
    }

    public Task<Dictionary<Guid, decimal>> GetAmountPaidMapAsync(List<Guid> planIds, CancellationToken ct) =>
        treatmentPlanRepository.GetPlanPaidMapAsync(planIds, ct);

    public async Task<decimal> GetAmountPaidAsync(Guid treatmentPlanId, CancellationToken ct) =>
        (await GetAmountPaidMapAsync(new List<Guid> { treatmentPlanId }, ct))
            .GetValueOrDefault(treatmentPlanId, 0m);

    /// <summary>
    /// Các liệu trình đã được xuất hóa đơn (hóa đơn chưa hoàn tiền) — kể cả hóa đơn chưa thanh toán,
    /// vì hóa đơn đã phát hành thì không được sửa danh mục dịch vụ nữa. Tập hợp này chính là các
    /// khóa (Key) của map "đã gắn hóa đơn" — cùng where-clause với GetPlanBilledMapAsync.
    /// </summary>
    public async Task<HashSet<Guid>> GetInvoicedPlanIdsAsync(List<Guid> planIds, CancellationToken ct) =>
        (await treatmentPlanRepository.GetPlanBilledMapAsync(planIds, ct)).Keys.ToHashSet();

    public async Task<bool> IsInvoicedAsync(Guid treatmentPlanId, CancellationToken ct) =>
        (await GetInvoicedPlanIdsAsync(new List<Guid> { treatmentPlanId }, ct)).Count > 0;

    /// <summary>Nạp lại một liệu trình kèm số tiền đã thu và cờ đã xuất hóa đơn.</summary>
    public async Task<TreatmentPlanDto> LoadDtoAsync(Guid planId, CancellationToken ct)
    {
        var plan = await treatmentPlanRepository.GetByIdWithDetailsAsync(planId, ct)
            ?? throw new NotFoundException("Không tìm thấy liệu trình điều trị.");

        var planIds = new List<Guid> { planId };
        var paid = (await GetAmountPaidMapAsync(planIds, ct)).GetValueOrDefault(planId, 0m);
        var isInvoiced = (await GetInvoicedPlanIdsAsync(planIds, ct)).Contains(planId);
        var stepNumbers = await GetProcedureStepNumbersAsync(plan.ServiceId, ct);

        return ClinicalRecordMappers.ToDto(plan, paid, isInvoiced, stepNumbers);
    }

    /// <summary>
    /// Chỉ ghi nhận/sửa quá trình điều trị khi bệnh nhân đang trong buổi khám
    /// (bác sĩ đã bấm "Bắt đầu khám") hoặc buổi khám đã kết thúc điều trị.
    /// </summary>
    public Task<bool> HasActiveVisitAsync(Guid patientId, CancellationToken ct) =>
        appointmentRepository.HasActiveVisitAsync(patientId, ct);
}
