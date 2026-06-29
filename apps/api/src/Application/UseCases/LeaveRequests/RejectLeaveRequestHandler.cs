using DentalClinic.API.Application.DTOs.LeaveRequests;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;

namespace DentalClinic.API.Application.UseCases.LeaveRequests;

public class RejectLeaveRequestHandler(
    ILeaveRequestRepository leaveRequestRepository,
    IActivityLogService activityLogService,
    ICurrentUserService currentUser)
{
    public async Task<LeaveRequestDto> HandleAsync(
        Guid id,
        RejectLeaveRequestRequest request,
        CancellationToken ct = default)
    {
        var leaveRequest = await leaveRequestRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Không tìm thấy đơn nghỉ phép với ID: {id}");

        leaveRequest.Reject(request.ReviewerNote);
        await leaveRequestRepository.UpdateAsync(leaveRequest, ct);

        await activityLogService.LogAsync(
            userId: currentUser.UserId,
            userName: currentUser.UserName,
            userRole: currentUser.UserRole,
            action: ActivityAction.Reject,
            module: ActivityModule.Leave,
            description: $"Từ chối đơn xin nghỉ ID: {id}",
            status: ActivityStatus.Success,
            ipAddress: currentUser.IpAddress,
            targetId: id.ToString(),
            ct: ct);

        return GetLeaveRequestsHandler.ToDto(leaveRequest);
    }
}
