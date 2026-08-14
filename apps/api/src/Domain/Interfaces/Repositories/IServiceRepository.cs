using DentalClinic.API.Domain.Entities;

namespace DentalClinic.API.Domain.Interfaces.Repositories;

public interface IServiceRepository
{
    Task<IEnumerable<Service>> GetAllAsync(CancellationToken ct = default);
    Task<Service?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Service service, CancellationToken ct = default);
    Task UpdateAsync(Service service, CancellationToken ct = default);
    Task DeleteAsync(Service service, CancellationToken ct = default);

    /// <summary>Dịch vụ đang hoạt động (IsActive), sắp theo tên — dùng cho chatbot/booking liệt kê dịch vụ khả dụng.</summary>
    Task<IEnumerable<Service>> GetActiveAsync(CancellationToken ct = default);
}
