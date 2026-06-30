using DentalClinic.API.Application.DTOs.ActivityLogs;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;

namespace DentalClinic.API.Application.UseCases.ActivityLogs;

public record GetActivityLogsQuery(
    Guid? UserId,
    string? Action,
    string? Module,
    string? Status,
    string? Search,
    DateTimeOffset? StartDate,
    DateTimeOffset? EndDate,
    int Page = 1,
    int PageSize = 20);

public class GetActivityLogsHandler(IActivityLogRepository repository, ICurrentUserService currentUser)
{
    public async Task<ActivityLogPagedDto> HandleAsync(GetActivityLogsQuery query, CancellationToken ct = default)
    {
        var targetUserId = query.UserId;

        // Security: If current user is not Admin or Owner, force filter to their own UserId
        if (currentUser.UserRole != "Admin" && currentUser.UserRole != "Owner")
        {
            targetUserId = currentUser.UserId;
        }

        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var page     = Math.Max(query.Page, 1);

        // If endDate has no time component (midnight), extend to end-of-day so the full day is included
        var endDate = query.EndDate.HasValue
            ? query.EndDate.Value.TimeOfDay == TimeSpan.Zero
                ? query.EndDate.Value.AddDays(1).AddTicks(-1)
                : query.EndDate.Value
            : (DateTimeOffset?)null;

        var (items, total) = await repository.GetPagedAsync(
            targetUserId,
            query.Action,
            query.Module,
            query.Status,
            query.Search,
            query.StartDate,
            endDate,
            page,
            pageSize,
            ct);

        var dtos = items.Select(a => new ActivityLogDto(
            a.Id,
            a.UserId,
            a.UserName,
            a.UserRole,
            a.Action,
            a.Module,
            a.Description,
            a.IpAddress,
            a.Status,
            a.TargetId,
            a.CreatedAt)).ToList();

        return new ActivityLogPagedDto(
            dtos,
            total,
            page,
            pageSize,
            (int)Math.Ceiling((double)total / pageSize));
    }
}
