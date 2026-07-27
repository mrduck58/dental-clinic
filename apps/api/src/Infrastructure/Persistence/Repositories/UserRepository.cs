using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Infrastructure.Persistence.Repositories;

public class UserRepository(AppDbContext db) : IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.Users
            .Include(u => u.Staff)
            .Include(u => u.Dentist)
            .Include(u => u.Patient)
            .FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        await db.Users
            .Include(u => u.Staff)
            .Include(u => u.Dentist)
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
            .Include(u => u.Staff)
            .Include(u => u.Dentist)
            .Include(u => u.Patient)
            .OrderBy(u => u.CreatedAt).ToListAsync(ct);

    public async Task<IEnumerable<User>> GetEmployeesWithoutAccountAsync(CancellationToken ct = default) =>
        await db.Users
            .Include(u => u.Staff)
            .Include(u => u.Dentist)
            .Where(u => u.Role != "Patient" && u.PasswordHash == null)
            .OrderBy(u => u.CreatedAt)
            .ToListAsync(ct);

    public async Task<(IReadOnlyList<User> Items, int TotalCount)> GetStaffPagedAsync(
        string? search, string? role, string? status,
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.Users
            .Include(u => u.Staff)
            .Include(u => u.Dentist)
            .Where(u => u.Role != "Patient")
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(u =>
                (u.FullName != null && u.FullName.ToLower().Contains(term)) ||
                (u.Email != null && u.Email.ToLower().Contains(term)) ||
                (u.Username != null && u.Username.ToLower().Contains(term)) ||
                (u.PhoneNumber != null && u.PhoneNumber.Contains(term)) ||
                (u.Staff != null && u.Staff.EmployeeId != null && u.Staff.EmployeeId.ToLower().Contains(term)) ||
                (u.Dentist != null && u.Dentist.EmployeeId != null && u.Dentist.EmployeeId.ToLower().Contains(term)) ||
                (u.Dentist != null && u.Dentist.LicenseNumber != null && u.Dentist.LicenseNumber.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            var roles = role.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            query = roles.Length == 1
                ? query.Where(u => u.Role == roles[0])
                : query.Where(u => roles.Contains(u.Role));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(u => 
                (u.Staff != null && u.Staff.EmploymentStatus == status) ||
                (u.Dentist != null && u.Dentist.EmploymentStatus == status));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<StaffStatsResult> GetStaffStatsAsync(CancellationToken ct = default)
    {
        var dentists = await db.Users.CountAsync(u => u.Role == "Dentist", ct);
        var doctors  = await db.Users.CountAsync(u => u.Role == "Doctor", ct);
        var staffs   = await db.Users.CountAsync(u => u.Role == "Staff", ct);
        return new StaffStatsResult(dentists + doctors + staffs, dentists, doctors);
    }

    public async Task<IReadOnlyList<Guid>> GetUserIdsByRoleAsync(string role, CancellationToken ct = default)
    {
        return await db.Users
            .Where(u => u.Role == role && u.IsActive)
            .Select(u => u.Id)
            .ToListAsync(ct);
    }
}
