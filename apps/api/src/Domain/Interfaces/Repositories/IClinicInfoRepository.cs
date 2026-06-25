using DentalClinic.API.Domain.Entities;

namespace DentalClinic.API.Domain.Interfaces.Repositories;

public interface IClinicInfoRepository
{
    /// <summary>Lấy dòng thông tin phòng khám (singleton — luôn lấy dòng đầu tiên).</summary>
    Task<ClinicInfo?> GetAsync(CancellationToken ct = default);
    Task AddAsync(ClinicInfo clinicInfo, CancellationToken ct = default);
    Task UpdateAsync(ClinicInfo clinicInfo, CancellationToken ct = default);
}
