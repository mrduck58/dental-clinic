using DentalClinic.API.Domain.Entities;

namespace DentalClinic.API.Domain.Interfaces.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> ExistsByUsernameAsync(string username, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
    Task UpdateAsync(User user, CancellationToken ct = default);
    Task<IEnumerable<User>> GetAllAsync(CancellationToken ct = default);
    Task<IEnumerable<User>> GetEmployeesWithoutAccountAsync(CancellationToken ct = default);
    Task<(IReadOnlyList<User> Items, int TotalCount)> GetStaffPagedAsync(
        string? search, string? role, string? status,
        int page, int pageSize, CancellationToken ct = default);
    Task<StaffStatsResult> GetStaffStatsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Guid>> GetUserIdsByRoleAsync(string role, CancellationToken ct = default);

    /// <summary>Lightweight lookup (no navigation-property joins) for per-request account-status checks.</summary>
    Task<AccountStatusResult?> GetAccountStatusAsync(Guid id, CancellationToken ct = default);
}

public record AccountStatusResult(bool IsActive, string Role);

public record StaffStatsResult(int TotalEmployees, int TotalDentists, int TotalDoctors);
