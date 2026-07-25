using DentalClinic.API.Application.DTOs.Services;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Domain.Constants;

namespace DentalClinic.API.Application.UseCases.Services;

public class UpdateServiceHandler(
    IServiceRepository serviceRepository,
    IActivityLogService activityLogService,
    ICurrentUserService currentUser)
{
    public async Task<ServiceDto> HandleAsync(Guid id, UpdateServiceRequest request, CancellationToken ct = default)
    {
        var service = await serviceRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Không tìm thấy dịch vụ với ID: {id}");

        service.Update(
            request.Name,
            request.Price,
            request.DurationMinutes,
            request.Description,
            request.ImageUrl,
            request.IconUrl);

        await serviceRepository.UpdateAsync(service, ct);

        await activityLogService.LogAsync(
            userId: currentUser.UserId,
            userName: currentUser.UserName,
            userRole: currentUser.UserRole,
            action: ActivityAction.Edit,
            module: ActivityModule.Service,
            description: $"Cập nhật dịch vụ: {service.Name}",
            status: ActivityStatus.Success,
            ipAddress: currentUser.IpAddress,
            targetId: id.ToString(),
            ct: ct);

        return new ServiceDto(
            service.Id, service.Name, service.Price,
            service.DurationMinutes, service.IsActive, service.Description,
            service.ViewCount, service.ImageUrl, service.IconUrl, service.CreatedAt, service.UpdatedAt);
    }
}
