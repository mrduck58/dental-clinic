using DentalClinic.API.Application.DTOs.LeaveRequests;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.LeaveRequests;

public record CancelLeaveRequestCommand(Guid Id, Guid RequestingUserId) : IRequest<LeaveRequestDto>;

public class CancelLeaveRequestHandler(ILeaveRequestRepository leaveRequestRepository) : IRequestHandler<CancelLeaveRequestCommand, LeaveRequestDto>
{
    public async Task<LeaveRequestDto> Handle(CancelLeaveRequestCommand command, CancellationToken ct)
    {
        var leaveRequest = await leaveRequestRepository.GetByIdAsync(command.Id, ct);

        // Cùng quy ước với GetLeaveRequestByIdHandler: đơn của người khác = không tồn tại (404).
        // Trước đây chỗ này ném ValidationException → trả 422, vừa sai ngữ nghĩa vừa xác nhận id có thật.
        if (leaveRequest is null || leaveRequest.UserId != command.RequestingUserId)
            throw new NotFoundException($"Không tìm thấy đơn nghỉ phép với ID: {command.Id}");

        leaveRequest.Cancel();
        await leaveRequestRepository.UpdateAsync(leaveRequest, ct);
        return GetLeaveRequestsHandler.ToDto(leaveRequest);
    }
}
