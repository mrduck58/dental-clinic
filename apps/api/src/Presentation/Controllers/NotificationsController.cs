using System.Security.Claims;
using DentalClinic.API.Application.UseCases.Notifications;
using DentalClinic.API.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.API.Presentation.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController(
    GetNotificationsHandler getNotificationsHandler,
    INotificationService notificationService) : ControllerBase
{
    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>GET api/notifications — Lấy danh sách thông báo của user hiện tại</summary>
    [HttpGet]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] string? type,
        [FromQuery] string? priority,
        [FromQuery] bool? isRead,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await getNotificationsHandler.HandleAsync(
            new GetNotificationsQuery(CurrentUserId, type, priority, isRead, search, page, pageSize),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>PUT api/notifications/{id}/read — Đánh dấu một thông báo đã đọc</summary>
    [HttpPut("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken cancellationToken)
    {
        await notificationService.MarkAsReadAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>PUT api/notifications/read-all — Đánh dấu tất cả thông báo đã đọc</summary>
    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
    {
        await notificationService.MarkAllAsReadAsync(CurrentUserId, cancellationToken);
        return NoContent();
    }

    /// <summary>DELETE api/notifications/{id} — Xóa một thông báo</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await notificationService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
