using DentalClinic.API.Domain.Entities;

namespace DentalClinic.API.Domain.Interfaces.Repositories;

public interface ITreatmentSupplyUsageRepository
{
    Task<IEnumerable<TreatmentSupplyUsage>> GetByTreatmentPlanIdAsync(Guid treatmentPlanId, CancellationToken ct = default);

    /// <summary>Các dòng tiêu hao CHƯA hoàn kho gắn với một mục cụ thể trong nhật ký điều trị — trả về entity
    /// đang được tracked để caller gọi MarkReversed() rồi lưu chung 1 lần với các thay đổi khác (AdjustQuantity...).</summary>
    Task<List<TreatmentSupplyUsage>> GetActiveByStepEntryIdAsync(Guid treatmentPlanId, Guid stepEntryId, CancellationToken ct = default);

    Task AddAsync(TreatmentSupplyUsage usage, CancellationToken ct = default);

    /// <summary>
    /// Mở transaction thủ công khi ghi nhận nhiều dòng vật tư tiêu hao cùng lúc (mỗi dòng kéo theo 1
    /// SupplyItem.AdjustQuantity + 1 SupplyTransaction + 1 TreatmentSupplyUsage) để không nửa vời nếu lỗi
    /// giữa chừng. Trả về null trên provider không hỗ trợ transaction thật (InMemory — dùng trong unit test).
    /// </summary>
    Task<ITreatmentSupplyUsageTransaction?> BeginTransactionAsync(CancellationToken ct = default);
}

public interface ITreatmentSupplyUsageTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken ct = default);
}
