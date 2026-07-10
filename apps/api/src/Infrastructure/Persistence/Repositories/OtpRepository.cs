using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Infrastructure.Persistence.Repositories;

public class OtpRepository(AppDbContext db) : IOtpRepository
{
    public async Task AddAsync(OtpCode otp, CancellationToken ct = default)
    {
        await db.OtpCodes.AddAsync(otp, ct);
        await db.SaveChangesAsync(ct);
    }

    public Task<OtpCode?> GetLatestValidAsync(string email, OtpPurpose purpose, CancellationToken ct = default)
        => db.OtpCodes
            .Where(o => o.Email == email && o.Purpose == purpose && !o.IsUsed && o.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(ct);

    public async Task InvalidateAllAsync(string email, OtpPurpose purpose, CancellationToken ct = default)
    {
        var pending = await db.OtpCodes
            .Where(o => o.Email == email && o.Purpose == purpose && !o.IsUsed)
            .ToListAsync(ct);

        foreach (var otp in pending)
            otp.MarkUsed();

        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(OtpCode otp, CancellationToken ct = default)
    {
        db.OtpCodes.Update(otp);
        await db.SaveChangesAsync(ct);
    }
}
