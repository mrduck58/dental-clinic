using DentalClinic.API.Domain.Entities;

namespace DentalClinic.API.Domain.Interfaces.Repositories;

public interface IActivityLogRepository
{
    Task AddAsync(ActivityLog log, CancellationToken ct = default);

    Task<(IReadOnlyList<ActivityLog> Items, int TotalCount)> GetPagedAsync(
        string? action,
        string? module,
        string? status,
        string? search,
        DateTimeOffset? startDate,
        DateTimeOffset? endDate,
        int page,
        int pageSize,
        CancellationToken ct = default);
}
