using DentalClinic.API.Application.DTOs.ClinicalRecords;
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
    IAppointmentRepository appointmentRepository)
{
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

        return ClinicalRecordMappers.ToDto(plan, paid, isInvoiced);
    }

    /// <summary>
    /// Chỉ ghi nhận/sửa quá trình điều trị khi bệnh nhân đang trong buổi khám
    /// (bác sĩ đã bấm "Bắt đầu khám") hoặc buổi khám đã kết thúc điều trị.
    /// </summary>
    public Task<bool> HasActiveVisitAsync(Guid patientId, CancellationToken ct) =>
        appointmentRepository.HasActiveVisitAsync(patientId, ct);
}
