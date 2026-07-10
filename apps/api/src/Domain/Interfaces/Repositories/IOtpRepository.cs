using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;

namespace DentalClinic.API.Domain.Interfaces.Repositories;

public interface IOtpRepository
{
    Task AddAsync(OtpCode otp, CancellationToken ct = default);
    Task<OtpCode?> GetLatestValidAsync(string email, OtpPurpose purpose, CancellationToken ct = default);
    Task InvalidateAllAsync(string email, OtpPurpose purpose, CancellationToken ct = default);
    Task UpdateAsync(OtpCode otp, CancellationToken ct = default);
}
