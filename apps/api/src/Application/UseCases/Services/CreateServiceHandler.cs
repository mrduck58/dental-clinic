using DentalClinic.API.Application.DTOs.Services;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
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
    string Content,
    string? ImageUrl,
    string? IconUrl,
    IReadOnlyCollection<ServiceOptionRequest>? Options,
    int? EstimatedSessionCount = null,
    int? EstimatedDurationMin = null,
    int? EstimatedDurationMax = null,
    string? EstimatedDurationUnit = null) : IRequest<ServiceDto>;

public class CreateServiceHandler(IServiceRepository serviceRepository, IActivityLogService activityLogService, ICurrentUserService currentUser) : IRequestHandler<CreateServiceCommand, ServiceDto>
{
    public async Task<ServiceDto> Handle(CreateServiceCommand request, CancellationToken cancellationToken)
    {
        DurationUnit? durationUnit = null;
        if (!string.IsNullOrWhiteSpace(request.EstimatedDurationUnit)
            && Enum.TryParse<DurationUnit>(request.EstimatedDurationUnit, ignoreCase: true, out var parsedUnit))
        {
            durationUnit = parsedUnit;
        }

        var service = Service.Create(
            request.Name,
            request.Price,
            request.DurationMinutes,
            request.Description,
            request.Content ?? string.Empty,
            request.ImageUrl,
            request.IconUrl,
            request.EstimatedSessionCount,
            request.EstimatedDurationMin,
            request.EstimatedDurationMax,
            durationUnit);

        // Add options if provided
        if (request.Options is { Count: > 0 })
        {
            foreach (var opt in request.Options.OrderBy(o => o.SortOrder))
            {
                service.AddOption(opt.Name, opt.Price, opt.Unit ?? "Răng", opt.SortOrder);
            }
        }

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

        return ServiceMapper.ToDto(service);
    }
}
