using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Infrastructure.Persistence.Repositories;

public class PatientRepository(AppDbContext dbContext) : IPatientRepository
{
    public async Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Patients
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Patient?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Patients
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
    }

    public async Task<IReadOnlyList<Patient>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Patients
            .AsNoTracking()
            .Include(p => p.User)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Patient patient, CancellationToken cancellationToken = default)
    {
        await dbContext.Patients.AddAsync(patient, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Patient patient, CancellationToken cancellationToken = default)
    {
        dbContext.Patients.Update(patient);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Patient patient, CancellationToken cancellationToken = default)
    {
        dbContext.Patients.Remove(patient);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Patient>> GetFamilyMembersAsync(Guid primaryPatientId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Patients
            .Include(p => p.User)
            .Where(p => p.PrimaryPatientId == primaryPatientId)
            .ToListAsync(cancellationToken);
    }

    public async Task<Patient?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        return await dbContext.Patients
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.User != null && p.User.PhoneNumber == phoneNumber, cancellationToken);
    }

    public async Task<IReadOnlyList<Patient>> SearchAsync(string term, int limit, bool onlyWithoutAccount = false, CancellationToken cancellationToken = default)
    {
        var needle = term.Trim().ToLower();
        if (needle.Length == 0 && !onlyWithoutAccount) return [];

        var query = dbContext.Patients.Include(p => p.User).AsQueryable();

        if (needle.Length > 0)
            query = query.Where(p =>
                (p.User.FullName != null && p.User.FullName.ToLower().Contains(needle)) ||
                (p.User.PhoneNumber != null && p.User.PhoneNumber.Contains(needle)));

        if (onlyWithoutAccount)
            query = query.Where(p => p.User.PasswordHash == null);

        // Duyệt danh sách (không gõ từ khóa) thì ưu tiên bệnh nhân mới thêm gần đây — sát nhu cầu
        // thực tế của staff hơn là sắp theo tên; có từ khóa thì vẫn giữ sắp theo tên như cũ.
        query = needle.Length == 0
            ? query.OrderByDescending(p => p.CreatedAt)
            : query.OrderBy(p => p.User.FullName);

        return await query.Take(limit).ToListAsync(cancellationToken);
    }
}
