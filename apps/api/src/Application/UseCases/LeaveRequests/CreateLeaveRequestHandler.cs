using DentalClinic.API.Application.DTOs.LeaveRequests;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Domain.Constants;

namespace DentalClinic.API.Application.UseCases.LeaveRequests;

public class CreateLeaveRequestHandler(
    ILeaveRequestRepository leaveRequestRepository,
    IActivityLogService activityLogService,
    ICurrentUserService currentUser)
{
    public async Task<LeaveRequestDto> HandleAsync(
        Guid userId,
        CreateLeaveRequestRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new ValidationException("Lý do nghỉ phép không được để trống.");

        if (request.Reason.Length > 1000)
            throw new ValidationException("Lý do nghỉ phép không được vượt quá 1000 ký tự.");

        if (!Enum.TryParse<LeaveType>(request.LeaveType, ignoreCase: true, out var leaveType))
            throw new ValidationException(
                $"Loại nghỉ phép không hợp lệ: '{request.LeaveType}'. " +
                "Hợp lệ: Annual, Sick, Maternity, Unpaid, Training.");

        var leaveRequest = LeaveRequest.Create(userId, leaveType, request.StartDate, request.EndDate, request.Reason);
        await leaveRequestRepository.AddAsync(leaveRequest, ct);

        await activityLogService.LogAsync(
            userId: currentUser.UserId,
            userName: currentUser.UserName,
            userRole: currentUser.UserRole,
            action: ActivityAction.Create,
            module: ActivityModule.Leave,
            description: $"Tạo đơn xin nghỉ: {leaveType} từ {request.StartDate:dd/MM/yyyy} đến {request.EndDate:dd/MM/yyyy}",
            status: ActivityStatus.Success,
            ipAddress: currentUser.IpAddress,
            targetId: leaveRequest.Id.ToString(),
            ct: ct);

        return GetLeaveRequestsHandler.ToDto(leaveRequest);
    }
}
