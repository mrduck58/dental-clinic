using DentalClinic.API.Application.DTOs.LeaveRequests;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;

namespace DentalClinic.API.Application.UseCases.LeaveRequests;

public class CancelLeaveRequestHandler(ILeaveRequestRepository leaveRequestRepository)
{
    public async Task<LeaveRequestDto> HandleAsync(Guid id, Guid requestingUserId, CancellationToken ct = default)
    {
        var leaveRequest = await leaveRequestRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Không tìm thấy đơn nghỉ phép với ID: {id}");

        if (leaveRequest.UserId != requestingUserId)
            throw new ValidationException("Bạn không có quyền hủy đơn nghỉ phép này.");

        leaveRequest.Cancel();
        await leaveRequestRepository.UpdateAsync(leaveRequest, ct);
        return GetLeaveRequestsHandler.ToDto(leaveRequest);
    }
}
