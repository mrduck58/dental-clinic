using DentalClinic.API.Application.DTOs.LeaveRequests;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;

namespace DentalClinic.API.Application.UseCases.LeaveRequests;

public class RejectLeaveRequestHandler(ILeaveRequestRepository leaveRequestRepository)
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
        return GetLeaveRequestsHandler.ToDto(leaveRequest);
    }
}
