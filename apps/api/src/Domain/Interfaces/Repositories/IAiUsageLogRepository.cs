using DentalClinic.API.Domain.Entities;

namespace DentalClinic.API.Domain.Interfaces.Repositories;

public interface IAiUsageLogRepository
{
    Task AddAsync(AiUsageLog log, CancellationToken ct = default);

    Task<IReadOnlyList<AiUsageLog>> GetSinceAsync(DateTimeOffset since, CancellationToken ct = default);
}
