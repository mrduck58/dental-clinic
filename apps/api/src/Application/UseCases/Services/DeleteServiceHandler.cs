using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Domain.Constants;

namespace DentalClinic.API.Application.UseCases.Services;

public class DeleteServiceHandler(
    IServiceRepository serviceRepository,
    IActivityLogService activityLogService,
    ICurrentUserService currentUser)
{
    public async Task HandleAsync(Guid id, CancellationToken ct = default)
    {
        var service = await serviceRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Không tìm thấy dịch vụ với ID: {id}");

        await serviceRepository.DeleteAsync(service, ct);

        await activityLogService.LogAsync(
            userId: currentUser.UserId,
            userName: currentUser.UserName,
            userRole: currentUser.UserRole,
            action: ActivityAction.Delete,
            module: ActivityModule.Service,
            description: $"Xóa dịch vụ: {service.Name}",
            status: ActivityStatus.Success,
            ipAddress: currentUser.IpAddress,
            targetId: id.ToString(),
            ct: ct);
    }
}
