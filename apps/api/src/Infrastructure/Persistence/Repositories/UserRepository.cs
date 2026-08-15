using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Infrastructure.Persistence.Repositories;

public class UserRepository(AppDbContext db) : IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.Users
            .Include(u => u.Employee).ThenInclude(e => e!.DentistProfile)
            .Include(u => u.Patient)
            .FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        await db.Users
            .Include(u => u.Employee).ThenInclude(e => e!.DentistProfile)
            .Include(u => u.Patient)
            .FirstOrDefaultAsync(u => u.Email == email, ct);

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default) =>
        await db.Users.AnyAsync(u => u.Email == email, ct);

    public async Task<AccountStatusResult?> GetAccountStatusAsync(Guid id, CancellationToken ct = default) =>
        await db.Users
            .Where(u => u.Id == id)
            .Select(u => new AccountStatusResult(u.IsActive, u.Role))
            .FirstOrDefaultAsync(ct);

    public async Task<bool> ExistsByUsernameAsync(string username, CancellationToken ct = default) =>
        await db.Users.AnyAsync(u => u.Username == username, ct);

    public async Task AddAsync(User user, CancellationToken ct = default)
    {
        await db.Users.AddAsync(user, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(User user, CancellationToken ct = default)
    {
        db.Users.Update(user);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<User>> GetAllAsync(CancellationToken ct = default) =>
        await db.Users
            .Include(u => u.Employee).ThenInclude(e => e!.DentistProfile)
            .Include(u => u.Patient)
            .OrderBy(u => u.CreatedAt).ToListAsync(ct);

    public async Task<(IReadOnlyList<User> Items, int TotalCount)> GetStaffPagedAsync(
        string? search, string? role, string? status, string? specialty,
        int page, int pageSize, string? sortBy = null, string? sortDir = null, CancellationToken ct = default)
    {
        var query = db.Users
            .Include(u => u.Employee).ThenInclude(e => e!.DentistProfile)
            .Where(u => u.Role != UserRole.Patient)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(u =>
                (u.FullName != null && u.FullName.ToLower().Contains(term)) ||
                (u.Email != null && u.Email.ToLower().Contains(term)) ||
                (u.Username != null && u.Username.ToLower().Contains(term)) ||
                (u.PhoneNumber != null && u.PhoneNumber.Contains(term)) ||
                (u.Employee != null && u.Employee.EmployeeId != null && u.Employee.EmployeeId.ToLower().Contains(term)) ||
                (u.Employee != null && u.Employee.DentistProfile != null &&
                 u.Employee.DentistProfile.LicenseNumber != null &&
                 u.Employee.DentistProfile.LicenseNumber.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            var roleNames = role.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var parsedRoles = roleNames
                .Select(r => Enum.TryParse<UserRole>(r, true, out var ur) ? (UserRole?)ur : null)
                .Where(r => r.HasValue)
                .Select(r => r!.Value)
                .ToArray();
            if (parsedRoles.Length > 0)
                query = query.Where(u => parsedRoles.Contains(u.Role));
        }

        if (!string.IsNullOrWhiteSpace(specialty))
        {
            // Một tham số dùng chung cho cả 2 tab: nha sĩ lọc theo chuyên khoa, nhân viên lọc theo
            // chức vụ / bộ phận. Dùng Contains để không vỡ khi giá trị lưu có hậu tố ("Lễ tân trưởng").
            var spec = specialty.Trim().ToLower();
            query = query.Where(u =>
                (u.Employee != null && u.Employee.DentistProfile != null &&
                 u.Employee.DentistProfile.Specialization != null &&
                 u.Employee.DentistProfile.Specialization.ToLower().Contains(spec)) ||
                (u.Employee != null && u.Employee.Position != null &&
                 u.Employee.Position.ToLower().Contains(spec)) ||
                (u.Employee != null && u.Employee.Department != null &&
                 u.Employee.Department.ToLower().Contains(spec)));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            // EmploymentStatus là cột NOT NULL; nhân sự chưa từng được set lưu chuỗi rỗng
            // chứ không phải null, và mặc định phải được coi là "Active" (giống các nơi khác
            // trong hệ thống dùng "?? Active"). Nếu không, lọc status=Active sẽ luôn ra rỗng.
            query = string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase)
                ? query.Where(u => u.Employee != null &&
                    (string.IsNullOrEmpty(u.Employee.EmploymentStatus) || u.Employee.EmploymentStatus == status))
                : query.Where(u => u.Employee != null && u.Employee.EmploymentStatus == status);
        }

        var total = await query.CountAsync(ct);

        var desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
        var ordered = sortBy?.ToLowerInvariant() switch
        {
            "department" => desc
                ? query.OrderByDescending(u => u.Employee != null ? u.Employee.Department : null)
                : query.OrderBy(u => u.Employee != null ? u.Employee.Department : null),
            // Chuyên khoa nha sĩ nằm ở DentistProfile.Specialization, không phải Employee.Department
            "specialty" => desc
                ? query.OrderByDescending(u => u.Employee != null && u.Employee.DentistProfile != null ? u.Employee.DentistProfile.Specialization : null)
                : query.OrderBy(u => u.Employee != null && u.Employee.DentistProfile != null ? u.Employee.DentistProfile.Specialization : null),
            "status" => desc
                ? query.OrderByDescending(u => u.Employee != null ? u.Employee.EmploymentStatus : null)
                : query.OrderBy(u => u.Employee != null ? u.Employee.EmploymentStatus : null),
            "salary" => desc
                ? query.OrderByDescending(u => u.Employee != null ? u.Employee.BaseSalary : null)
                : query.OrderBy(u => u.Employee != null ? u.Employee.BaseSalary : null),
            "leaveaccrued" => desc
                ? query.OrderByDescending(u => u.Employee != null ? u.Employee.LeaveAccrued : null)
                : query.OrderBy(u => u.Employee != null ? u.Employee.LeaveAccrued : null),
            _ => desc
                ? query.OrderByDescending(u => u.FullName)
                : query.OrderBy(u => u.FullName),
        };

        var items = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<StaffStatsResult> GetStaffStatsAsync(CancellationToken ct = default)
    {
        var dentists = await db.Users.CountAsync(u => u.Role == UserRole.Dentist, ct);
        var staffs   = await db.Users.CountAsync(u => u.Role == UserRole.Staff, ct);
        return new StaffStatsResult(dentists + staffs, dentists, 0);
    }

    public async Task<IReadOnlyList<Guid>> GetUserIdsByRoleAsync(string role, CancellationToken ct = default)
    {
        if (!Enum.TryParse<UserRole>(role, true, out var userRole))
            return [];

        return await db.Users
            .Where(u => u.Role == userRole && u.IsActive)
            .Select(u => u.Id)
            .ToListAsync(ct);
    }
}
