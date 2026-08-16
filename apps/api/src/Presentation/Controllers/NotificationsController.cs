using DentalClinic.API.Application.UseCases.Notifications;
using DentalClinic.API.Domain.Interfaces.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.API.Presentation.Controllers;

public record RegisterDeviceTokenDto(string? Token, string? DeviceToken, string? DeviceType)
{
    public string EffectiveToken => !string.IsNullOrWhiteSpace(Token) ? Token : (DeviceToken ?? string.Empty);
}

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController(
    ISender sender,
    INotificationService notificationService,
    ICurrentUserService currentUser,
    IFirebasePushNotificationService? pushNotificationService = null) : ControllerBase
{
    private Guid CurrentUserId =>
        currentUser.UserId ?? throw new UnauthorizedAccessException("Không xác định được người dùng từ token.");

    /// <summary>POST api/notifications/device-token — Đăng ký token FCM của thiết bị</summary>
    [HttpPost("device-token")]
    public async Task<IActionResult> RegisterDeviceToken([FromBody] RegisterDeviceTokenDto dto, CancellationToken cancellationToken)
    {
        var token = dto.EffectiveToken;
        if (pushNotificationService != null && !string.IsNullOrWhiteSpace(token))
        {
            await pushNotificationService.RegisterTokenAsync(CurrentUserId, token, dto.DeviceType, cancellationToken);
        }
        return Ok(new { message = "Đã lưu device token thành công." });
    }

    /// <summary>GET api/notifications — Lấy danh sách thông báo của user hiện tại</summary>
    [HttpGet]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] string? type,
        [FromQuery] string? priority,
        [FromQuery] bool? isRead,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sortDir = null,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new GetNotificationsQuery(CurrentUserId, type, priority, isRead, search, page, pageSize, sortDir),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>PUT api/notifications/{id}/read — Đánh dấu một thông báo đã đọc</summary>
    [HttpPut("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken cancellationToken)
    {
        await notificationService.MarkAsReadAsync(id, CurrentUserId, cancellationToken);
        return Ok(new { message = "Đã đánh dấu thông báo đã đọc." });
    }

    /// <summary>PUT api/notifications/read-all — Đánh dấu tất cả thông báo đã đọc</summary>
    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
    {
        await notificationService.MarkAllAsReadAsync(CurrentUserId, cancellationToken);
        return Ok(new { message = "Đã đánh dấu tất cả thông báo đã đọc." });
    }

    /// <summary>DELETE api/notifications/{id} — Xóa một thông báo</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await notificationService.DeleteAsync(id, CurrentUserId, cancellationToken);
        return Ok(new { message = "Đã xóa thông báo." });
    }

    /// <summary>DELETE api/notifications — Xóa tất cả thông báo của user hiện tại</summary>
    [HttpDelete]
    public async Task<IActionResult> DeleteAll(CancellationToken cancellationToken)
    {
        await notificationService.DeleteAllAsync(CurrentUserId, cancellationToken);
        return Ok(new { message = "Đã xóa tất cả thông báo." });
    }
}
