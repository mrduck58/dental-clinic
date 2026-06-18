using DentalClinic.API.Application.DTOs.LeaveRequests;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;

namespace DentalClinic.API.Application.UseCases.LeaveRequests;

public class GetLeaveRequestsHandler(ILeaveRequestRepository leaveRequestRepository)
{
    public async Task<IEnumerable<LeaveRequestDto>> HandleAsync(
        string? status,
        string? search,
        CancellationToken ct = default)
    {
        var requests = await leaveRequestRepository.GetAllAsync(ct);

        if (!string.IsNullOrWhiteSpace(status))
            requests = requests.Where(r =>
                r.Status.ToString().Equals(status, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.ToLower();
            requests = requests.Where(r =>
                (r.User.FullName ?? string.Empty).ToLower().Contains(q) ||
                r.Reason.ToLower().Contains(q));
        }

        return requests.Select(ToDto);
    }

    internal static LeaveRequestDto ToDto(LeaveRequest r) => new(
        r.Id,
        r.UserId,
        r.User.FullName ?? r.User.Email,
        r.User.Department,
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
