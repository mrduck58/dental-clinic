using DentalClinic.API.Application.DTOs.LeaveRequests;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Schedules;
using MediatR;

namespace DentalClinic.API.Application.UseCases.LeaveRequests;

public record CreateLeaveRequestCommand(Guid UserId, CreateLeaveRequestRequest Request) : IRequest<LeaveRequestDto>;

public class CreateLeaveRequestHandler(
    ILeaveRequestRepository leaveRequestRepository,
    IActivityLogService activityLogService,
    INotificationService notificationService,
    IUserRepository userRepository,
    ICurrentUserService currentUser) : IRequestHandler<CreateLeaveRequestCommand, LeaveRequestDto>
{
    public async Task<LeaveRequestDto> Handle(CreateLeaveRequestCommand command, CancellationToken ct)
    {
        var request = command.Request;

        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new ValidationException("Lý do nghỉ phép không được để trống.");

        if (request.Reason.Length > 1000)
            throw new ValidationException("Lý do nghỉ phép không được vượt quá 1000 ký tự.");

        if (!Enum.TryParse<LeaveType>(request.LeaveType, ignoreCase: true, out var leaveType))
            throw new ValidationException(
                $"Loại nghỉ phép không hợp lệ: '{request.LeaveType}'. " +
                "Hợp lệ: Annual, Sick, Maternity, Unpaid, Training.");

        if (request.Shifts == null || request.Shifts.Count == 0)
            throw new ValidationException("Vui lòng chọn ít nhất một ca muốn nghỉ.");

        var invalidShift = request.Shifts.FirstOrDefault(s => !WorkShifts.AllValidCodes.Contains(s.ShiftId, StringComparer.OrdinalIgnoreCase));
        if (invalidShift != null)
            throw new ValidationException($"Mã ca không hợp lệ: '{invalidShift.ShiftId}'.");

        var shiftTuples = request.Shifts.Select(s => (s.Date, s.ShiftId)).ToList();
        var leaveRequest = LeaveRequest.Create(command.UserId, leaveType, shiftTuples, request.Reason);
        await leaveRequestRepository.AddAsync(leaveRequest, ct);

        await activityLogService.LogAsync(
            userId: currentUser.UserId,
            userName: currentUser.UserName,
            userRole: currentUser.UserRole,
            action: ActivityAction.Create,
            module: ActivityModule.Leave,
            description: $"Tạo đơn xin nghỉ: {leaveType} — {leaveRequest.Shifts.Count} ca ({leaveRequest.DaysCount} ngày, {leaveRequest.StartDate:dd/MM/yyyy} – {leaveRequest.EndDate:dd/MM/yyyy})",
            status: ActivityStatus.Success,
            ipAddress: currentUser.IpAddress,
            targetId: leaveRequest.Id.ToString(),
            ct: ct);

        var adminIds = await userRepository.GetUserIdsByRoleAsync("Owner", ct);
        var template = new CreateNotificationRequest(
            UserId: Guid.Empty,
            Type: NotificationType.Schedule,
            Priority: NotificationPriority.Medium,
            Title: "Đơn xin nghỉ mới",
            Body: $"{currentUser.UserName} đã nộp đơn xin nghỉ {leaveRequest.Shifts.Count} ca từ {leaveRequest.StartDate:dd/MM/yyyy} đến {leaveRequest.EndDate:dd/MM/yyyy}.",
            RelatedEntityType: "LeaveRequest",
            RelatedEntityId: leaveRequest.Id.ToString());
        await notificationService.CreateForMultipleUsersAsync(adminIds, template, ct);

        return GetLeaveRequestsHandler.ToDto(leaveRequest);
    }
}
