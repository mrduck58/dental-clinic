using DentalClinic.API.Domain.Entities;

namespace DentalClinic.API.Domain.Interfaces.Repositories;

public interface IServiceSupplyItemRepository
{
    /// <summary>Toàn bộ định mức của dịch vụ (mọi option) — dùng cho màn quản lý (Admin).</summary>
    Task<IEnumerable<ServiceSupplyItem>> GetByServiceIdAsync(Guid serviceId, CancellationToken ct = default);

    /// <summary>
    /// Định mức HIỆU LỰC khi đã biết option cụ thể — gồm các dòng dùng CHUNG (ServiceOptionName null)
    /// cộng các dòng khai riêng cho đúng option đó. optionName null/rỗng → chỉ trả các dòng dùng chung.
    /// </summary>
    Task<IEnumerable<ServiceSupplyItem>> GetEffectiveByServiceIdAsync(Guid serviceId, string? optionName, CancellationToken ct = default);

    Task ReplaceAllForServiceAsync(Guid serviceId, IEnumerable<ServiceSupplyItem> newItems, CancellationToken ct = default);
}
