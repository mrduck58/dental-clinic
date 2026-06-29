using DentalClinic.API.Application.DTOs.LeaveRequests;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;

namespace DentalClinic.API.Application.UseCases.LeaveRequests;

public class ApproveLeaveRequestHandler(
    ILeaveRequestRepository leaveRequestRepository,
    IActivityLogService activityLogService,
    ICurrentUserService currentUser)
{
    public async Task<LeaveRequestDto> HandleAsync(Guid id, CancellationToken ct = default)
    {
        var request = await leaveRequestRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Không tìm thấy đơn nghỉ phép với ID: {id}");

        request.Approve();
        await leaveRequestRepository.UpdateAsync(request, ct);

        await activityLogService.LogAsync(
            userId: currentUser.UserId,
            userName: currentUser.UserName,
            userRole: currentUser.UserRole,
            action: ActivityAction.Approve,
            module: ActivityModule.Leave,
            description: $"Duyệt đơn xin nghỉ ID: {id}",
            status: ActivityStatus.Success,
            ipAddress: currentUser.IpAddress,
            targetId: id.ToString(),
            ct: ct);

        return GetLeaveRequestsHandler.ToDto(request);
    }
}
