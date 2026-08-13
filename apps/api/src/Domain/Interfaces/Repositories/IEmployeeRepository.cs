using DentalClinic.API.Domain.Entities;

namespace DentalClinic.API.Domain.Interfaces.Repositories;

public interface IEmployeeRepository
{
    Task<Employee?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Employee?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Kèm DentistProfile — dùng khi cần biết luôn user đó có phải bác sĩ hay không.</summary>
    Task<Employee?> GetByUserIdWithDentistProfileAsync(Guid userId, CancellationToken ct = default);

    Task AddAsync(Employee employee, CancellationToken ct = default);
    Task UpdateAsync(Employee employee, CancellationToken ct = default);
}
