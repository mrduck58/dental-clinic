using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;

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
    /// <param name="specialty">Chuyên khoa (nha sĩ) hoặc chức vụ / bộ phận (nhân viên).</param>
    /// <param name="sortBy">"name" | "department" | "specialty" | "status" | "salary" | "leaveAccrued" — mặc định "name" (họ tên, tăng dần).</param>
    /// <param name="sortDir">"asc" | "desc" — mặc định "asc".</param>
    Task<(IReadOnlyList<User> Items, int TotalCount)> GetStaffPagedAsync(
        string? search, string? role, string? status, string? specialty,
        int page, int pageSize, string? sortBy = null, string? sortDir = null, CancellationToken ct = default);
    Task<StaffStatsResult> GetStaffStatsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Guid>> GetUserIdsByRoleAsync(string role, CancellationToken ct = default);

    /// <summary>Lightweight lookup (no navigation-property joins) for per-request account-status checks.</summary>
    Task<AccountStatusResult?> GetAccountStatusAsync(Guid id, CancellationToken ct = default);
}

public record AccountStatusResult(bool IsActive, UserRole Role);

public record StaffStatsResult(int TotalEmployees, int TotalDentists, int TotalDoctors);
