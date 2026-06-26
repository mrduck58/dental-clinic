using DentalClinic.API.Application.DTOs.ClinicInfo;
using DentalClinic.API.Domain.Interfaces.Repositories;
using Entity = DentalClinic.API.Domain.Entities.ClinicInfo;

namespace DentalClinic.API.Application.UseCases.ClinicInfo;

public class UpdateClinicInfoHandler(IClinicInfoRepository repository)
{
    /// <summary>
    /// Cập nhật thông tin phòng khám. Nếu chưa có dòng nào (chưa seed) thì tạo mới.
    /// Danh sách nào null trong request sẽ được giữ nguyên (không ghi đè).
    /// </summary>
    public async Task<ClinicInfoDto> HandleAsync(UpdateClinicInfoRequest request, CancellationToken ct = default)
    {
        var info = await repository.GetAsync(ct);

        if (info is null)
        {
            info = Entity.Create(
                request.AboutTitle,
                request.AboutDescription,
                request.FoundedYear,
                request.Phone,
                request.Email,
                request.Address,
                request.AboutImageUrl);
            ApplyCollections(info, request);
            await repository.AddAsync(info, ct);
        }
        else
        {
            info.UpdateContent(
                request.AboutTitle,
                request.AboutDescription,
                request.FoundedYear,
                request.Phone,
                request.Email,
                request.Address,
                request.AboutImageUrl);
            ApplyCollections(info, request);
            await repository.UpdateAsync(info, ct);
        }

        return ClinicInfoMapper.ToDto(info);
    }

    private static void ApplyCollections(Entity info, UpdateClinicInfoRequest request) =>
        info.SetCollections(
            request.Milestones is null ? null : ClinicInfoMapper.Serialize(request.Milestones),
            request.Certifications is null ? null : ClinicInfoMapper.Serialize(request.Certifications),
            request.Features is null ? null : ClinicInfoMapper.Serialize(request.Features),
            request.TreatmentSteps is null ? null : ClinicInfoMapper.Serialize(request.TreatmentSteps),
            request.Statistics is null ? null : ClinicInfoMapper.Serialize(request.Statistics));
}
