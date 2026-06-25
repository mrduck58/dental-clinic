using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Infrastructure.Persistence.Repositories;

public class ClinicInfoRepository(AppDbContext db) : IClinicInfoRepository
{
    public async Task<ClinicInfo?> GetAsync(CancellationToken ct = default)
        => await db.ClinicInfos
            .OrderBy(c => c.CreatedAt)
            .FirstOrDefaultAsync(ct);

    public async Task AddAsync(ClinicInfo clinicInfo, CancellationToken ct = default)
    {
        await db.ClinicInfos.AddAsync(clinicInfo, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(ClinicInfo clinicInfo, CancellationToken ct = default)
    {
        db.ClinicInfos.Update(clinicInfo);
        await db.SaveChangesAsync(ct);
    }
}
