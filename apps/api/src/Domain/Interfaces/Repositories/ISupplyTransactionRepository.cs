using DentalClinic.API.Domain.Entities;

namespace DentalClinic.API.Domain.Interfaces.Repositories;

public interface ISupplyTransactionRepository
{
    Task<IEnumerable<SupplyTransaction>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(SupplyTransaction transaction, CancellationToken ct = default);
}
