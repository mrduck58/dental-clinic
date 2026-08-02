using DentalClinic.API.Application.DTOs.LeaveRequests;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.LeaveRequests;

public record GetLeaveRequestByIdQuery(Guid Id) : IRequest<LeaveRequestDto>;

public class GetLeaveRequestByIdHandler(ILeaveRequestRepository leaveRequestRepository) : IRequestHandler<GetLeaveRequestByIdQuery, LeaveRequestDto>
{
    public async Task<LeaveRequestDto> Handle(GetLeaveRequestByIdQuery query, CancellationToken ct)
    {
        var request = await leaveRequestRepository.GetByIdAsync(query.Id, ct)
            ?? throw new NotFoundException($"Không tìm thấy đơn nghỉ phép với ID: {query.Id}");

        return GetLeaveRequestsHandler.ToDto(request);
    }
}
