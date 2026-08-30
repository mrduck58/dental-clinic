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

    public async Task<IReadOnlyList<Patient>> GetFamilyByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        var matchedByPhone = await dbContext.Patients
            .Include(p => p.User)
            .Include(p => p.PrimaryPatient)
                .ThenInclude(pp => pp.User)
            .Where(p => p.User != null && p.User.PhoneNumber == phoneNumber)
            .ToListAsync(cancellationToken);

        if (matchedByPhone.Count == 0)
        {
            return [];
        }

        var primaryIds = matchedByPhone.Select(p => p.Id).ToList();
        var parentIds = matchedByPhone.Where(p => p.PrimaryPatientId != null).Select(p => p.PrimaryPatientId!.Value).ToList();
        var allRelatedPrimaryIds = primaryIds.Union(parentIds).Distinct().ToList();

        var familyMembers = await dbContext.Patients
            .Include(p => p.User)
            .Include(p => p.PrimaryPatient)
                .ThenInclude(pp => pp.User)
            .Where(p => p.PrimaryPatientId != null && allRelatedPrimaryIds.Contains(p.PrimaryPatientId.Value))
            .ToListAsync(cancellationToken);

        var resultDict = new Dictionary<Guid, Patient>();
        foreach (var p in matchedByPhone) resultDict[p.Id] = p;
        foreach (var fm in familyMembers) resultDict[fm.Id] = fm;

        return resultDict.Values.ToList();
    }

    public async Task<IReadOnlyList<Patient>> SearchAsync(string term, int limit, bool onlyWithoutAccount = false, CancellationToken cancellationToken = default)
    {
        var needle = term.Trim().ToLower();
        if (needle.Length == 0 && !onlyWithoutAccount) return [];

        var query = dbContext.Patients
            .Include(p => p.User)
            .Include(p => p.PrimaryPatient)
                .ThenInclude(pp => pp.User)
            .AsQueryable();

        if (needle.Length > 0)
            query = query.Where(p =>
                (p.User.FullName != null && p.User.FullName.ToLower().Contains(needle)) ||
                (p.User.PhoneNumber != null && p.User.PhoneNumber.Contains(needle)) ||
                (p.PrimaryPatient != null && p.PrimaryPatient.User.PhoneNumber != null && p.PrimaryPatient.User.PhoneNumber.Contains(needle)));

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
