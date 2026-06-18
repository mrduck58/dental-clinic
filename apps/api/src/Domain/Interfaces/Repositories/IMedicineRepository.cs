using DentalClinic.API.Domain.Entities;

namespace DentalClinic.API.Domain.Interfaces.Repositories;

public interface IMedicineRepository
{
    Task<IEnumerable<Medicine>> GetAllAsync(CancellationToken ct = default);
    Task<Medicine?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Medicine medicine, CancellationToken ct = default);
    Task UpdateAsync(Medicine medicine, CancellationToken ct = default);
    Task DeleteAsync(Medicine medicine, CancellationToken ct = default);
}
