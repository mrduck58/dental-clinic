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
        var leaveRequest = await leaveRequestRepository.GetByIdAsync(command.Id, ct)
            ?? throw new NotFoundException($"Không tìm thấy đơn nghỉ phép với ID: {command.Id}");

        if (leaveRequest.UserId != command.RequestingUserId)
            throw new ValidationException("Bạn không có quyền hủy đơn nghỉ phép này.");

        leaveRequest.Cancel();
        await leaveRequestRepository.UpdateAsync(leaveRequest, ct);
        return GetLeaveRequestsHandler.ToDto(leaveRequest);
    }
}
