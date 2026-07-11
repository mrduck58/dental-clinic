using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DentalClinic.API.Application.UseCases.Chat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.API.Presentation.Controllers;

[ApiController]
[Route("api/chat")]
[Authorize(Roles = "Patient")]
public class ChatController(
    StartConversationHandler startConversationHandler,
    SendChatMessageHandler sendChatMessageHandler,
    GetMyConversationsHandler getMyConversationsHandler,
    GetConversationMessagesHandler getConversationMessagesHandler) : ControllerBase
{
    /// <summary>POST api/chat/conversations — Bắt đầu một cuộc trò chuyện mới với chatbot AI</summary>
    [HttpPost("conversations")]
    public async Task<IActionResult> StartConversation(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await startConversationHandler.HandleAsync(userId, cancellationToken);
        return Ok(result);
    }

    /// <summary>GET api/chat/conversations — Danh sách cuộc trò chuyện của bệnh nhân hiện tại</summary>
    [HttpGet("conversations")]
    public async Task<IActionResult> GetMyConversations(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await getMyConversationsHandler.HandleAsync(userId, cancellationToken);
        return Ok(result);
    }

    /// <summary>GET api/chat/conversations/{id} — Toàn bộ tin nhắn của một cuộc trò chuyện</summary>
    [HttpGet("conversations/{id:guid}")]
    public async Task<IActionResult> GetConversation(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await getConversationMessagesHandler.HandleAsync(userId, id, cancellationToken);
        return Ok(result);
    }

    /// <summary>POST api/chat/conversations/{id}/messages — Gửi tin nhắn cho chatbot AI</summary>
    [HttpPost("conversations/{id:guid}/messages")]
    public async Task<IActionResult> SendMessage(
        Guid id, [FromBody] SendChatMessageRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await sendChatMessageHandler.HandleAsync(userId, id, request.Message, cancellationToken);
        return Ok(result);
    }

    private Guid GetCurrentUserId()
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("Không xác định được người dùng từ token.");
        return Guid.Parse(sub);
    }
}

public record SendChatMessageRequest(string Message);
