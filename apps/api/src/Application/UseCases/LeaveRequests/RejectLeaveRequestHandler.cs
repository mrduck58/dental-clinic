using DentalClinic.API.Application.DTOs.LeaveRequests;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Domain.Constants;

namespace DentalClinic.API.Application.UseCases.LeaveRequests;

public class RejectLeaveRequestHandler(
    ILeaveRequestRepository leaveRequestRepository,
    IActivityLogService activityLogService,
    INotificationService notificationService,
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

        await notificationService.CreateAsync(new CreateNotificationRequest(
            UserId: leaveRequest.UserId,
            Type: NotificationType.Schedule,
            Priority: NotificationPriority.High,
            Title: "Đơn xin nghỉ bị từ chối",
            Body: $"Đơn xin nghỉ từ {leaveRequest.StartDate:dd/MM/yyyy} đến {leaveRequest.EndDate:dd/MM/yyyy} đã bị từ chối." +
                  (string.IsNullOrWhiteSpace(leaveRequest.ReviewerNote) ? "" : $" Lý do: {leaveRequest.ReviewerNote}"),
            RelatedEntityType: "LeaveRequest",
            RelatedEntityId: leaveRequest.Id.ToString()), ct);

        return GetLeaveRequestsHandler.ToDto(leaveRequest);
    }
}
