using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DentalClinic.API.Application.DTOs.Schedules;
using DentalClinic.API.Application.UseCases.Schedules;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.API.Presentation.Controllers;

[ApiController]
[Route("api/schedules")]
public class SchedulesController(ISender sender) : ControllerBase
{
    /// <summary>GET api/schedules?weekStart=YYYY-MM-DD — Lấy lịch làm việc toàn phòng khám theo tuần
    /// (Owner: quản lý nhân sự; Admin: chỉ xem để biết tình trạng phòng ở trang Quản lí phòng,
    /// không có quyền lưu/sửa lịch — xem SaveWeek bên dưới vẫn chỉ dành cho Owner).</summary>
    [HttpGet]
    [Authorize(Roles = "Owner,Admin")]
    public async Task<IActionResult> GetByWeek([FromQuery] string weekStart, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(weekStart))
            return BadRequest("weekStart is required (format: YYYY-MM-DD)");

        var result = await sender.Send(new GetWeekScheduleQuery(weekStart), ct);
        return Ok(result);
    }

    /// <summary>GET api/schedules/my?weekStart=YYYY-MM-DD — Lịch làm việc của nha sĩ đang đăng nhập (chỉ xem)</summary>
    [HttpGet("my")]
    [Authorize(Roles = "Dentist")]
    public async Task<IActionResult> GetMySchedule([FromQuery] string weekStart, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(weekStart))
            return BadRequest("weekStart is required (format: YYYY-MM-DD)");

        var result = await sender.Send(new GetMyScheduleQuery(GetCurrentUserId(), weekStart), ct);
        return Ok(result);
    }

    /// <summary>PUT api/schedules/week/{weekStart} — Lưu toàn bộ lịch làm việc cho một tuần (Owner, thay thế)</summary>
    [HttpPut("week/{weekStart}")]
    [Authorize(Roles = "Owner")]
    public async Task<IActionResult> SaveWeek(string weekStart, [FromBody] SaveWeekScheduleRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new SaveWeekScheduleCommand(weekStart, request), ct);
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
