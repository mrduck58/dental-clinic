using DentalClinic.API.Application.DTOs.LeaveRequests;
using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using MediatR;

namespace DentalClinic.API.Application.UseCases.LeaveRequests;

public record ApproveLeaveRequestCommand(Guid Id) : IRequest<LeaveRequestDto>;

public class ApproveLeaveRequestHandler(
    ILeaveRequestRepository leaveRequestRepository,
    IActivityLogService activityLogService,
    INotificationService notificationService,
    ICurrentUserService currentUser) : IRequestHandler<ApproveLeaveRequestCommand, LeaveRequestDto>
{
    public async Task<LeaveRequestDto> Handle(ApproveLeaveRequestCommand command, CancellationToken ct)
    {
        var request = await leaveRequestRepository.GetByIdAsync(command.Id, ct)
            ?? throw new NotFoundException($"Không tìm thấy đơn nghỉ phép với ID: {command.Id}");

        request.Approve();
        await leaveRequestRepository.UpdateAsync(request, ct);

        await activityLogService.LogAsync(
            userId: currentUser.UserId,
            userName: currentUser.UserName,
            userRole: currentUser.UserRole,
            action: ActivityAction.Approve,
            module: ActivityModule.Leave,
            description: $"Duyệt đơn xin nghỉ ID: {command.Id}",
            status: ActivityStatus.Success,
            ipAddress: currentUser.IpAddress,
            targetId: command.Id.ToString(),
            ct: ct);

        await notificationService.CreateAsync(new CreateNotificationRequest(
            UserId: request.UserId,
            Type: NotificationType.Schedule,
            Priority: NotificationPriority.Medium,
            Title: "Đơn xin nghỉ được phê duyệt",
            Body: $"Đơn xin nghỉ từ {request.StartDate:dd/MM/yyyy} đến {request.EndDate:dd/MM/yyyy} đã được duyệt.",
            RelatedEntityType: "LeaveRequest",
            RelatedEntityId: request.Id.ToString()), ct);

        return GetLeaveRequestsHandler.ToDto(request);
    }
}
