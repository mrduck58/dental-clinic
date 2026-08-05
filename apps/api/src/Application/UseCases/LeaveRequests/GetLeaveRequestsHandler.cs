using DentalClinic.API.Application.DTOs.LeaveRequests;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.LeaveRequests;

public record GetLeaveRequestsQuery(string? Status, string? Search) : IRequest<IEnumerable<LeaveRequestDto>>;

public class GetLeaveRequestsHandler(ILeaveRequestRepository leaveRequestRepository) : IRequestHandler<GetLeaveRequestsQuery, IEnumerable<LeaveRequestDto>>
{
    public async Task<IEnumerable<LeaveRequestDto>> Handle(GetLeaveRequestsQuery query, CancellationToken ct)
    {
        var requests = await leaveRequestRepository.GetAllAsync(ct);

        if (!string.IsNullOrWhiteSpace(query.Status))
            requests = requests.Where(r =>
                r.Status.ToString().Equals(query.Status, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var q = query.Search.ToLower();
            requests = requests.Where(r =>
                (r.User.FullName ?? string.Empty).ToLower().Contains(q) ||
                r.Reason.ToLower().Contains(q));
        }

        return requests.Select(ToDto);
    }

    internal static LeaveRequestDto ToDto(LeaveRequest r) => new(
        r.Id,
        r.UserId,
        !string.IsNullOrWhiteSpace(r.User.FullName) ? r.User.FullName : (r.User.Email ?? string.Empty),
        r.User.Employee?.Department,
        r.LeaveType.ToString(),
        r.StartDate,
        r.EndDate,
        r.DaysCount,
        r.Reason,
        r.Status.ToString(),
        r.ReviewerNote,
        r.CreatedAt,
        r.ReviewedAt);
}
