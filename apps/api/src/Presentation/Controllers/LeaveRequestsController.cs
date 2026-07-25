using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DentalClinic.API.Application.DTOs.LeaveRequests;
using DentalClinic.API.Application.UseCases.LeaveRequests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.API.Presentation.Controllers;

[ApiController]
[Route("api/leave-requests")]
[Authorize]
public class LeaveRequestsController(
    GetLeaveRequestsHandler getAll,
    GetMyLeaveRequestsHandler getMy,
    GetLeaveRequestByIdHandler getById,
    CreateLeaveRequestHandler create,
    ApproveLeaveRequestHandler approve,
    RejectLeaveRequestHandler reject,
    CancelLeaveRequestHandler cancel) : ControllerBase
{
    /// <summary>GET api/leave-requests — Tất cả đơn nghỉ (Admin)</summary>
    [HttpGet]
    [Authorize(Roles = "Owner")]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        [FromQuery] string? search,
        CancellationToken ct)
    {
        var result = await getAll.HandleAsync(status, search, ct);
        return Ok(result);
    }

    /// <summary>GET api/leave-requests/my — Đơn nghỉ của tôi + thống kê (Dentist/Staff)</summary>
    [HttpGet("my")]
    public async Task<IActionResult> GetMy(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var result = await getMy.HandleAsync(userId, ct);
        return Ok(result);
    }

    /// <summary>GET api/leave-requests/{id} — Chi tiết đơn nghỉ</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await getById.HandleAsync(id, ct);
        return Ok(result);
    }

    /// <summary>POST api/leave-requests — Tạo đơn xin nghỉ (Dentist/Staff)</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLeaveRequestRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var result = await create.HandleAsync(userId, request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>PUT api/leave-requests/{id}/approve — Duyệt đơn nghỉ (Admin)</summary>
    [HttpPut("{id:guid}/approve")]
    [Authorize(Roles = "Owner")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
    {
        var result = await approve.HandleAsync(id, ct);
        return Ok(result);
    }

    /// <summary>PUT api/leave-requests/{id}/reject — Từ chối đơn nghỉ (Admin)</summary>
    [HttpPut("{id:guid}/reject")]
    [Authorize(Roles = "Owner")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectLeaveRequestRequest request, CancellationToken ct)
    {
        var result = await reject.HandleAsync(id, request, ct);
        return Ok(result);
    }

    /// <summary>PUT api/leave-requests/{id}/cancel — Hủy đơn nghỉ (chính chủ, chỉ khi Pending)</summary>
    [HttpPut("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var result = await cancel.HandleAsync(id, userId, ct);
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
