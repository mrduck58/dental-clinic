using DentalClinic.API.Domain.Entities;

namespace DentalClinic.API.Domain.Interfaces.Repositories;

public interface ICommissionRuleRepository
{
    Task<IReadOnlyList<CommissionRule>> GetAllAsync(CancellationToken ct = default);

    Task<CommissionRule?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task AddAsync(CommissionRule rule, CancellationToken ct = default);
    Task DeleteAsync(CommissionRule rule, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
