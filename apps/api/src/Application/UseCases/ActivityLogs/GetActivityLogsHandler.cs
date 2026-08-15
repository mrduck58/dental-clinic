using DentalClinic.API.Application.DTOs.ActivityLogs;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using MediatR;

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
    int PageSize = 20,
    string? SortDir = null) : IRequest<ActivityLogPagedDto>;

public class GetActivityLogsHandler(IActivityLogRepository repository, ICurrentUserService currentUser)
    : IRequestHandler<GetActivityLogsQuery, ActivityLogPagedDto>
{
    public async Task<ActivityLogPagedDto> Handle(GetActivityLogsQuery query, CancellationToken ct)
    {
        var targetUserId = query.UserId;

        // Security: If current user is not Admin or Owner, force filter to their own UserId
        if (currentUser.UserRole != "Admin" && currentUser.UserRole != "Owner")
        {
            targetUserId = currentUser.UserId;
        }

        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var page     = Math.Max(query.Page, 1);

        // Client gửi mốc ngày theo giờ VN (offset +07:00). Npgsql chỉ chấp nhận DateTimeOffset với
        // Offset=0, nên phải quy về UTC trước khi xuống repository — nếu không query sẽ ném lỗi.
        var startDate = query.StartDate?.ToUniversalTime();

        // If endDate has no time component (midnight), extend to end-of-day so the full day is included.
        // Phải mở rộng TRƯỚC khi quy về UTC vì TimeOfDay được tính theo offset client gửi.
        var endDate = query.EndDate.HasValue
            ? (query.EndDate.Value.TimeOfDay == TimeSpan.Zero
                ? query.EndDate.Value.AddDays(1).AddTicks(-1)
                : query.EndDate.Value).ToUniversalTime()
            : (DateTimeOffset?)null;

        var (items, total) = await repository.GetPagedAsync(
            targetUserId,
            query.Action,
            query.Module,
            query.Status,
            query.Search,
            startDate,
            endDate,
            page,
            pageSize,
            query.SortDir,
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
