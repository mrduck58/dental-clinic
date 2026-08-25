using DentalClinic.API.Application.DTOs.Invoices;
using DentalClinic.API.Domain.Interfaces.Repositories;

namespace DentalClinic.API.Application.UseCases.Invoices;

/// <summary>Truy vấn/tính toán dùng chung giữa nhiều handler Invoice và <see cref="PaymentConfirmationService"/> —
/// tách ra để không phải lặp lại logic công nợ liệu trình ở từng handler nhỏ sau khi InvoiceHandler được chia tách.
/// Phần công nợ liệu trình (paid/billed map) ủy quyền cho <see cref="ITreatmentPlanRepository"/> — tránh lặp lại
/// y hệt logic này ở <c>TreatmentPlanQueryHelper</c> bên ClinicalRecords. Truy vấn hóa đơn ủy quyền cho
/// <see cref="IInvoiceRepository"/> — nơi thực sự chạm EF Core/AppDbContext.</summary>
public class InvoiceQueryHelper(IInvoiceRepository invoiceRepository, ITreatmentPlanRepository treatmentPlanRepository)
{
    public async Task<InvoiceDto> GetByIdAsync(Guid invoiceId, CancellationToken ct)
    {
        var invoice = await invoiceRepository.GetByIdWithDetailsAsync(invoiceId, ct)
            ?? throw new InvalidOperationException($"Không tìm thấy hóa đơn {invoiceId}.");
        return InvoiceHelpers.ToDto(invoice);
    }

    /// <summary>Tổng số tiền đã thu (Paid) của một liệu trình.</summary>
    public async Task<decimal> GetPlanPaidAsync(Guid treatmentPlanId, CancellationToken ct) =>
        (await GetPlanPaidMapAsync(new List<Guid> { treatmentPlanId }, ct)).GetValueOrDefault(treatmentPlanId, 0m);

    /// <summary>
    /// Map số tiền ĐÃ THU theo từng liệu trình. Mỗi dòng hóa đơn đã Paid credit đúng số thu của dòng
    /// (AmountCollected); nếu hóa đơn cọc đã tất toán thì credit trọn thành tiền dòng.
    /// Cũng gộp các hóa đơn "đợt thu" cũ (gắn liệu trình ở cấp hóa đơn).
    /// </summary>
    public Task<Dictionary<Guid, decimal>> GetPlanPaidMapAsync(List<Guid> planIds, CancellationToken ct) =>
        treatmentPlanRepository.GetPlanPaidMapAsync(planIds, ct);

    /// <summary>
    /// Map số tiền ĐÃ GẮN vào hóa đơn (chưa hủy) theo từng liệu trình — để không xuất hóa đơn trùng.
    /// Gồm dòng hóa đơn gắn liệu trình (mô hình mới) và hóa đơn "đợt thu" cũ (gắn ở cấp hóa đơn).
    /// </summary>
    public Task<Dictionary<Guid, decimal>> GetPlanBilledMapAsync(List<Guid> planIds, CancellationToken ct) =>
        treatmentPlanRepository.GetPlanBilledMapAsync(planIds, ct);

    /// <summary>Chuỗi tái khám của một buổi hẹn: chính nó + các buổi gốc phía trên (đi ngược FollowUpFromAppointmentId).</summary>
    public Task<HashSet<Guid>> GetChainAsync(Guid appointmentId, CancellationToken ct) =>
        invoiceRepository.GetFollowUpChainAsync(appointmentId, ct);
}
