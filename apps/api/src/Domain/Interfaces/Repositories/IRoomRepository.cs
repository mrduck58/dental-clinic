using DentalClinic.API.Domain.Entities;

namespace DentalClinic.API.Domain.Interfaces.Repositories;

public interface IRoomRepository
{
    Task<IEnumerable<Room>> GetAllAsync(CancellationToken ct = default);
    Task<Room?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsByCodeAsync(string code, Guid? excludeId = null, CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken ct = default);
    Task AddAsync(Room room, CancellationToken ct = default);
    Task UpdateAsync(Room room, CancellationToken ct = default);
    Task DeleteAsync(Room room, CancellationToken ct = default);
}
