using DentalClinic.API.Application.DTOs.LeaveRequests;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;

namespace DentalClinic.API.Application.UseCases.LeaveRequests;

public class CreateLeaveRequestHandler(ILeaveRequestRepository leaveRequestRepository)
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

        // LeaveRequest.Create already validates endDate >= startDate
        var leaveRequest = LeaveRequest.Create(userId, leaveType, request.StartDate, request.EndDate, request.Reason);

        // Repository loads the User navigation property after saving
        await leaveRequestRepository.AddAsync(leaveRequest, ct);

        return GetLeaveRequestsHandler.ToDto(leaveRequest);
    }
}
