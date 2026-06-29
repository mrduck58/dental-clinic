using DentalClinic.API.Application.DTOs.Services;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;

namespace DentalClinic.API.Application.UseCases.Services;

public class CreateServiceHandler(IServiceRepository serviceRepository, IActivityLogService activityLogService, ICurrentUserService currentUser)
{
    public async Task<ServiceDto> HandleAsync(CreateServiceRequest request, CancellationToken ct = default)
    {
        var service = Service.Create(
            request.Name,
            request.Price,
            request.DurationMinutes,
            request.Description,
            request.ImageUrl);

        await serviceRepository.AddAsync(service, ct);

        await activityLogService.LogAsync(
            userId: currentUser.UserId,
            userName: currentUser.UserName,
            userRole: currentUser.UserRole,
            action: ActivityAction.Create,
            module: ActivityModule.Service,
            description: $"Tạo dịch vụ mới: {request.Name}",
            status: ActivityStatus.Success,
            ipAddress: currentUser.IpAddress,
            targetId: service.Id.ToString(),
            ct: ct);

        return new ServiceDto(
            service.Id, service.Name, service.Price,
            service.DurationMinutes, service.IsActive, service.Description,
            service.ViewCount, service.ImageUrl, service.CreatedAt, service.UpdatedAt);
    }
}
