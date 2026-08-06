using DentalClinic.API.Domain.Entities;

namespace DentalClinic.API.Domain.Interfaces.Repositories;

public interface IPromotionRepository
{
    Task<IEnumerable<Promotion>> GetAllAsync(CancellationToken ct = default);
    Task<Promotion?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Promotion promotion, CancellationToken ct = default);
    Task UpdateAsync(Promotion promotion, CancellationToken ct = default);
    Task DeleteAsync(Promotion promotion, CancellationToken ct = default);

    /// <summary>Ưu đãi đang hoạt động và còn trong hạn tại một ngày cụ thể, sắp theo ngày kết thúc — dùng cho chatbot.</summary>
    Task<IEnumerable<Promotion>> GetActiveOnDateAsync(DateOnly date, CancellationToken ct = default);
}
