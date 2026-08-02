using DentalClinic.API.Application.DTOs.ClinicInfo;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.ClinicInfo;

public record GetClinicInfoQuery : IRequest<ClinicInfoDto?>;

public class GetClinicInfoHandler(IClinicInfoRepository repository) : IRequestHandler<GetClinicInfoQuery, ClinicInfoDto?>
{
    /// <summary>Trả về thông tin phòng khám, hoặc null nếu chưa được seed.</summary>
    public async Task<ClinicInfoDto?> Handle(GetClinicInfoQuery request, CancellationToken ct)
    {
        var info = await repository.GetAsync(ct);
        return info is null ? null : ClinicInfoMapper.ToDto(info);
    }
}
