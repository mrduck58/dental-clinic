using DentalClinic.API.Domain.Entities;

namespace DentalClinic.API.Domain.Interfaces.Repositories;

public interface ISupplyItemRepository
{
    Task<IEnumerable<SupplyItem>> GetAllAsync(CancellationToken ct = default);
    Task<SupplyItem?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(SupplyItem item, CancellationToken ct = default);
    Task UpdateAsync(SupplyItem item, CancellationToken ct = default);
    Task DeleteAsync(SupplyItem item, CancellationToken ct = default);

    /// <summary>True nếu đã có vật tư với mã (Code) này — dùng để chặn tạo trùng mã.</summary>
    Task<bool> ExistsByCodeAsync(string code, CancellationToken ct = default);

    /// <summary>Tìm vật tư theo tên (không phân biệt hoa/thường) — dùng khi nhập kho để gộp vào vật tư đã có
    /// thay vì tạo mới. Trả về entity đang được tracked (không AsNoTracking) để caller có thể chỉnh sửa trực
    /// tiếp (AdjustQuantity/UpdatePrice) và lưu chung 1 lần với thao tác khác trong cùng DbContext.</summary>
    Task<SupplyItem?> GetByNameAsync(string name, CancellationToken ct = default);
}
