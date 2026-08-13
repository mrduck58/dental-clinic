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
    string Content,
    string? ImageUrl,
    string? IconUrl,
    IReadOnlyCollection<ServiceOptionRequest>? Options) : IRequest<ServiceDto>;

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
            request.Content ?? string.Empty,
            request.ImageUrl,
            request.IconUrl);

        // Replace options: delete existing via repository, then add new ones through domain entity
        await serviceRepository.DeleteOptionsAsync(service.Id, cancellationToken);

        if (request.Options is { Count: > 0 })
        {
            var newOptions = request.Options
                .OrderBy(o => o.SortOrder)
                .Select((o, i) => (o.Name, o.Price, Unit: o.Unit ?? "Răng", SortOrder: i));
            service.ReplaceOptions(newOptions);
        }

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

        // Reload to get fresh options list
        var updated = await serviceRepository.GetByIdAsync(service.Id, cancellationToken) ?? service;
        return ServiceMapper.ToDto(updated);
    }
}
