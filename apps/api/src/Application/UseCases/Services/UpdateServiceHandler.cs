using DentalClinic.API.Application.DTOs.Services;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Domain.Constants;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Services;

public record UpdateServiceCommand(
    Guid Id,
    string Name,
    decimal Price,
    int DurationMinutes,
    string Description,
    string? ImageUrl,
    string? IconUrl) : IRequest<ServiceDto>;

public class UpdateServiceHandler(
    IServiceRepository serviceRepository,
    IActivityLogService activityLogService,
    ICurrentUserService currentUser) : IRequestHandler<UpdateServiceCommand, ServiceDto>
{
    public async Task<ServiceDto> Handle(UpdateServiceCommand request, CancellationToken cancellationToken)
    {
        var service = await serviceRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Không tìm thấy dịch vụ với ID: {request.Id}");

        service.Update(
            request.Name,
            request.Price,
            request.DurationMinutes,
            request.Description,
            request.ImageUrl,
            request.IconUrl);

        await serviceRepository.UpdateAsync(service, cancellationToken);

        await activityLogService.LogAsync(
            userId: currentUser.UserId,
            userName: currentUser.UserName,
            userRole: currentUser.UserRole,
            action: ActivityAction.Edit,
            module: ActivityModule.Service,
            description: $"Cập nhật dịch vụ: {service.Name}",
            status: ActivityStatus.Success,
            ipAddress: currentUser.IpAddress,
            targetId: request.Id.ToString(),
            ct: cancellationToken);

        return new ServiceDto(
            service.Id, service.Name, service.Price,
            service.DurationMinutes, service.IsActive, service.Description,
            service.ViewCount, service.ImageUrl, service.IconUrl, service.CreatedAt, service.UpdatedAt);
    }
}
