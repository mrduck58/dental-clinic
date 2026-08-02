using DentalClinic.API.Domain.Entities;

namespace DentalClinic.API.Domain.Interfaces.Repositories;

/// <summary>
/// Liệu trình điều trị (mỗi dòng là một dịch vụ chỉ định cho bệnh nhân) và công nợ của nó.
/// Hai method <c>GetPlanPaidMapAsync</c>/<c>GetPlanBilledMapAsync</c> nằm ở đây vì đứng từ góc nhìn
/// "biết công nợ của liệu trình" — trước đây logic này bị viết trùng ở cả InvoiceQueryHelper
/// lẫn TreatmentPlanQueryHelper.
/// </summary>
public interface ITreatmentPlanRepository
{
    Task<TreatmentPlan?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Đọc để hiển thị (AsNoTracking) kèm dịch vụ và bác sĩ phụ trách.</summary>
    Task<TreatmentPlan?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default);

    /// <summary>Đọc để GHI (có tracking) kèm bác sĩ phụ trách — cần tên bác sĩ khi ghi nhật ký điều trị.</summary>
    Task<TreatmentPlan?> GetByIdWithDentistAsync(Guid id, CancellationToken ct = default);

    /// <summary>Tất cả liệu trình của một bệnh nhân (mọi buổi hẹn), cũ nhất trước.</summary>
    Task<IReadOnlyList<TreatmentPlan>> GetByPatientIdAsync(Guid patientId, CancellationToken ct = default);

    Task AddAsync(TreatmentPlan treatmentPlan, CancellationToken ct = default);
    Task UpdateAsync(TreatmentPlan treatmentPlan, CancellationToken ct = default);
    Task DeleteAsync(TreatmentPlan treatmentPlan, CancellationToken ct = default);

    /// <summary>
    /// Map số tiền ĐÃ THU theo từng liệu trình. Mỗi dòng hóa đơn đã Paid credit đúng số thu của dòng
    /// (AmountCollected); nếu hóa đơn cọc đã tất toán thì credit trọn thành tiền dòng.
    /// Cũng gộp các hóa đơn "đợt thu" cũ (gắn liệu trình ở cấp hóa đơn).
    /// </summary>
    Task<Dictionary<Guid, decimal>> GetPlanPaidMapAsync(List<Guid> planIds, CancellationToken ct = default);

    /// <summary>
    /// Map số tiền ĐÃ GẮN vào hóa đơn (chưa hoàn tiền) theo từng liệu trình — để không xuất hóa đơn trùng.
    /// Gồm dòng hóa đơn gắn liệu trình (mô hình mới) và hóa đơn "đợt thu" cũ (gắn ở cấp hóa đơn).
    /// </summary>
    Task<Dictionary<Guid, decimal>> GetPlanBilledMapAsync(List<Guid> planIds, CancellationToken ct = default);
}
