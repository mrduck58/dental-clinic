using DentalClinic.API.Domain.Entities;

namespace DentalClinic.API.Domain.Interfaces.Repositories;

public interface ISupplyTransactionRepository
{
    Task<IEnumerable<SupplyTransaction>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(SupplyTransaction transaction, CancellationToken ct = default);

    /// <summary>
    /// Nhập kho: thêm <paramref name="newItem"/> nếu khác null (vật tư chưa từng có trong kho), rồi tạo
    /// <paramref name="transaction"/> — lưu 1 lần duy nhất (SaveChanges) để atomic. Trường hợp vật tư đã tồn
    /// tại (newItem == null), entity đó phải đã được chỉnh sửa (AdjustQuantity/UpdatePrice) và đang được
    /// tracked bởi cùng DbContext từ trước (xem ISupplyItemRepository.GetByNameAsync) — EF Core tự nhận thay
    /// đổi khi SaveChanges mà không cần gọi Update() lại.
    /// </summary>
    Task AddImportAsync(SupplyItem? newItem, SupplyTransaction transaction, CancellationToken ct = default);
}
