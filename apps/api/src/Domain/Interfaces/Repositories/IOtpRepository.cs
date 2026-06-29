using DentalClinic.API.Domain.Entities;

namespace DentalClinic.API.Domain.Interfaces.Repositories;

public interface IOtpRepository
{
    Task AddAsync(OtpCode otp, CancellationToken ct = default);
    Task<OtpCode?> GetLatestValidAsync(string email, CancellationToken ct = default);
Task InvalidateAllAsync(string email, CancellationToken ct = default);
    Task UpdateAsync(OtpCode otp, CancellationToken ct = default);
}
