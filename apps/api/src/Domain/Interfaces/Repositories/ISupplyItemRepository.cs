using DentalClinic.API.Domain.Entities;

namespace DentalClinic.API.Domain.Interfaces.Repositories;

public interface ISupplyItemRepository
{
    Task<IEnumerable<SupplyItem>> GetAllAsync(CancellationToken ct = default);
    Task<SupplyItem?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(SupplyItem item, CancellationToken ct = default);
    Task UpdateAsync(SupplyItem item, CancellationToken ct = default);
}
