using DentalClinic.API.Domain.Entities;

namespace DentalClinic.API.Domain.Interfaces.Repositories;

public interface IStaffRepository
{
    Task<Staff?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Staff?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task AddAsync(Staff staff, CancellationToken ct = default);
    Task UpdateAsync(Staff staff, CancellationToken ct = default);
}
