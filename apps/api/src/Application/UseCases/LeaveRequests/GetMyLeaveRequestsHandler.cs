using DentalClinic.API.Application.DTOs.LeaveRequests;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.LeaveRequests;

public record GetMyLeaveRequestsQuery(Guid UserId) : IRequest<MyLeaveRequestsResponse>;

public class GetMyLeaveRequestsHandler(ILeaveRequestRepository leaveRequestRepository) : IRequestHandler<GetMyLeaveRequestsQuery, MyLeaveRequestsResponse>
{
    private const int TotalAnnualDays = 12;

    public async Task<MyLeaveRequestsResponse> Handle(GetMyLeaveRequestsQuery query, CancellationToken ct)
    {
        var requests = (await leaveRequestRepository.GetByUserIdAsync(query.UserId, ct)).ToList();

        var currentYear = DateTimeOffset.UtcNow.Year;

        var usedAnnualDays = requests
            .Where(r => r.LeaveType == LeaveType.Annual
                && r.Status == LeaveStatus.Approved
                && r.StartDate.Year == currentYear)
            .Sum(r => r.DaysCount);

        var stats = new MyLeaveStatsDto(
            TotalAnnualDays,
            usedAnnualDays,
            TotalAnnualDays - usedAnnualDays,
            requests.Count(r => r.Status == LeaveStatus.Pending),
            requests.Count(r => r.Status == LeaveStatus.Approved && r.StartDate.Year == currentYear));

        return new MyLeaveRequestsResponse(stats, requests.Select(GetLeaveRequestsHandler.ToDto));
    }
}
