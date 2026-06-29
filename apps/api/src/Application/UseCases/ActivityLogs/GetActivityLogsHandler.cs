using DentalClinic.API.Application.DTOs.ActivityLogs;
using DentalClinic.API.Domain.Interfaces.Repositories;

namespace DentalClinic.API.Application.UseCases.ActivityLogs;

public record GetActivityLogsQuery(
    string? Action,
    string? Module,
    string? Status,
    string? Search,
    DateTimeOffset? StartDate,
    DateTimeOffset? EndDate,
    int Page = 1,
    int PageSize = 20);

public class GetActivityLogsHandler(IActivityLogRepository repository)
{
    public async Task<ActivityLogPagedDto> HandleAsync(GetActivityLogsQuery query, CancellationToken ct = default)
    {
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var page     = Math.Max(query.Page, 1);

        var (items, total) = await repository.GetPagedAsync(
            query.Action,
            query.Module,
            query.Status,
            query.Search,
            query.StartDate,
            query.EndDate,
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
