using DentalClinic.API.Application.DTOs.Notifications;
using DentalClinic.API.Domain.Interfaces.Repositories;

namespace DentalClinic.API.Application.UseCases.Notifications;

public record GetNotificationsQuery(
    Guid UserId,
    string? Type = null,
    string? Priority = null,
    bool? IsRead = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 10);

public class GetNotificationsHandler(INotificationRepository repository)
{
    public async Task<NotificationPagedDto> HandleAsync(GetNotificationsQuery query, CancellationToken ct = default)
    {
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var page     = Math.Max(query.Page, 1);

        var (items, total) = await repository.GetPagedAsync(
            query.UserId,
            query.Type,
            query.Priority,
            query.IsRead,
            query.Search,
            page,
            pageSize,
            ct);

        var unreadCount = await repository.GetUnreadCountAsync(query.UserId, ct);

        var dtos = items.Select(n => new NotificationDto(
            n.Id,
            n.Type,
            n.Priority,
            n.Title,
            n.Body,
            n.IsRead,
            n.ReadAt,
            n.RelatedEntityType,
            n.RelatedEntityId,
            n.CreatedAt)).ToList();

        return new NotificationPagedDto(
            dtos,
            total,
            page,
            pageSize,
            (int)Math.Ceiling((double)total / pageSize),
            unreadCount);
    }
}
