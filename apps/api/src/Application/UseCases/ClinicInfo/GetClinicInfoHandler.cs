using DentalClinic.API.Application.DTOs.ClinicInfo;
using DentalClinic.API.Domain.Interfaces.Repositories;

namespace DentalClinic.API.Application.UseCases.ClinicInfo;

public class GetClinicInfoHandler(IClinicInfoRepository repository)
{
    /// <summary>Trả về thông tin phòng khám, hoặc null nếu chưa được seed.</summary>
    public async Task<ClinicInfoDto?> HandleAsync(CancellationToken ct = default)
    {
        var info = await repository.GetAsync(ct);
        return info is null ? null : ClinicInfoMapper.ToDto(info);
    }
}
