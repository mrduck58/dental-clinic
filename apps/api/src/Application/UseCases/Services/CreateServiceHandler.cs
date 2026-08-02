using DentalClinic.API.Application.DTOs.Services;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Domain.Constants;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Services;

public record CreateServiceCommand(
    string Name,
    decimal Price,
    int DurationMinutes,
    string Description,
    string? ImageUrl,
    string? IconUrl) : IRequest<ServiceDto>;

public class CreateServiceHandler(IServiceRepository serviceRepository, IActivityLogService activityLogService, ICurrentUserService currentUser) : IRequestHandler<CreateServiceCommand, ServiceDto>
{
    public async Task<ServiceDto> Handle(CreateServiceCommand request, CancellationToken cancellationToken)
    {
        var service = Service.Create(
            request.Name,
            request.Price,
            request.DurationMinutes,
            request.Description,
            request.ImageUrl,
            request.IconUrl);

        await serviceRepository.AddAsync(service, cancellationToken);

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
            ct: cancellationToken);

        return new ServiceDto(
            service.Id, service.Name, service.Price,
            service.DurationMinutes, service.IsActive, service.Description,
            service.ViewCount, service.ImageUrl, service.IconUrl, service.CreatedAt, service.UpdatedAt);
    }
}
