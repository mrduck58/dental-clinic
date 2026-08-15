using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DentalClinic.API.Application.DTOs.LeaveRequests;
using DentalClinic.API.Application.UseCases.LeaveRequests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.API.Presentation.Controllers;

[ApiController]
[Route("api/leave-requests")]
// Nghỉ phép là việc nội bộ của nhân sự. Trước đây class chỉ có [Authorize] nên bệnh nhân đăng nhập
// cũng nộp và đọc được đơn nghỉ — liệt kê role ở đây loại Patient ra khỏi toàn bộ controller.
[Authorize(Roles = "Owner,Admin,Dentist,Staff")]
public class LeaveRequestsController(ISender sender) : ControllerBase
{
    /// <summary>GET api/leave-requests — Tất cả đơn nghỉ (Admin)</summary>
    [HttpGet]
    [Authorize(Roles = "Owner")]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        [FromQuery] string? search,
        CancellationToken ct)
    {
        var result = await sender.Send(new GetLeaveRequestsQuery(status, search), ct);
        return Ok(result);
    }

    /// <summary>GET api/leave-requests/my — Đơn nghỉ của tôi + thống kê (Dentist/Staff)</summary>
    [HttpGet("my")]
    public async Task<IActionResult> GetMy(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var result = await sender.Send(new GetMyLeaveRequestsQuery(userId), ct);
        return Ok(result);
    }

    /// <summary>GET api/leave-requests/{id} — Chi tiết đơn nghỉ (chính chủ, hoặc Owner duyệt đơn)</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(
            new GetLeaveRequestByIdQuery(id, GetCurrentUserId(), CanViewAll: User.IsInRole("Owner")),
            ct);
        return Ok(result);
    }

    /// <summary>GET api/leave-requests/{id}/impact — Ảnh hưởng của đơn nghỉ tới lịch làm việc
    /// và lịch hẹn (Owner xem trước khi duyệt: duyệt xong các ca này sẽ bị gỡ khỏi lịch).</summary>
    [HttpGet("{id:guid}/impact")]
    [Authorize(Roles = "Owner")]
    public async Task<IActionResult> GetImpact(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetLeaveRequestImpactQuery(id), ct);
        return Ok(result);
    }

    /// <summary>POST api/leave-requests — Tạo đơn xin nghỉ (Dentist/Staff)</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLeaveRequestRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var result = await sender.Send(new CreateLeaveRequestCommand(userId, request), ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>PUT api/leave-requests/{id}/approve — Duyệt đơn nghỉ (Owner).
    /// Kèm theo: gỡ các ca đã xếp cho người này trong khoảng nghỉ và báo Owner bổ sung lịch.</summary>
    [HttpPut("{id:guid}/approve")]
    [Authorize(Roles = "Owner")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new ApproveLeaveRequestCommand(id), ct);
        return Ok(result);
    }

    /// <summary>PUT api/leave-requests/{id}/reject — Từ chối đơn nghỉ (Admin)</summary>
    [HttpPut("{id:guid}/reject")]
    [Authorize(Roles = "Owner")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectLeaveRequestRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new RejectLeaveRequestCommand(id, request), ct);
        return Ok(result);
    }

    /// <summary>PUT api/leave-requests/{id}/cancel — Hủy đơn nghỉ (chính chủ, chỉ khi Pending)</summary>
    [HttpPut("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var result = await sender.Send(new CancelLeaveRequestCommand(id, userId), ct);
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
