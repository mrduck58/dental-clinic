using DentalClinic.API.Application.DTOs.LeaveRequests;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;

namespace DentalClinic.API.Application.UseCases.LeaveRequests;

public class GetLeaveRequestByIdHandler(ILeaveRequestRepository leaveRequestRepository)
{
    public async Task<LeaveRequestDto> HandleAsync(Guid id, CancellationToken ct = default)
    {
        var request = await leaveRequestRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Không tìm thấy đơn nghỉ phép với ID: {id}");

        return GetLeaveRequestsHandler.ToDto(request);
    }
}
