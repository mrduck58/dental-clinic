using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DentalClinic.API.Application.DTOs.Payrolls;
using DentalClinic.API.Application.UseCases.Payrolls;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.API.Presentation.Controllers;

[ApiController]
[Route("api/payrolls")]
public class PayrollsController(ISender sender) : ControllerBase
{
    /// <summary>GET api/payrolls?year=&amp;month= — Bảng lương của một kỳ (Owner)</summary>
    [HttpGet]
    [Authorize(Roles = "Owner")]
    public async Task<IActionResult> GetPeriod(
        [FromQuery] int? year,
        [FromQuery] int? month,
        [FromQuery] string? search,
        [FromQuery] string? department,
        [FromQuery] string? role,
        CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var result = await sender.Send(
            new GetPayrollPeriodQuery(year ?? today.Year, month ?? today.Month, search, department, role), ct);
        return Ok(result);
    }

    /// <summary>GET api/payrolls/yearly?year= — Báo cáo quỹ lương cả năm, 12 kỳ (Owner)</summary>
    [HttpGet("yearly")]
    [Authorize(Roles = "Owner")]
    public async Task<IActionResult> GetYearly([FromQuery] int? year, CancellationToken ct)
    {
        var result = await sender.Send(new GetPayrollYearlyQuery(year ?? DateTime.UtcNow.Year), ct);
        return Ok(result);
    }

    /// <summary>GET api/payrolls/me?year=&amp;month= — Bảng lương của chính người đang đăng nhập (Dentist/Staff)</summary>
    [HttpGet("me")]
    [Authorize(Roles = "Dentist,Staff")]
    public async Task<IActionResult> GetMyPeriod([FromQuery] int? year, [FromQuery] int? month, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var result = await sender.Send(
            new GetMyPayrollPeriodQuery(GetCurrentUserId(), year ?? today.Year, month ?? today.Month), ct);
        return Ok(result);
    }

    /// <summary>GET api/payrolls/me/yearly?year= — Diễn biến lương 12 tháng của chính người đang đăng nhập (Dentist/Staff)</summary>
    [HttpGet("me/yearly")]
    [Authorize(Roles = "Dentist,Staff")]
    public async Task<IActionResult> GetMyYearly([FromQuery] int? year, CancellationToken ct)
    {
        var result = await sender.Send(new GetMyPayrollYearlyQuery(GetCurrentUserId(), year ?? DateTime.UtcNow.Year), ct);
        return Ok(result);
    }

    /// <summary>PUT api/payrolls/pay — Chi trả lương một nhân sự trong kỳ (Owner)</summary>
    [HttpPut("pay")]
    [Authorize(Roles = "Owner")]
    public async Task<IActionResult> Pay([FromBody] PayPayrollRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new PayPayrollCommand(request.Year, request.Month, request.UserId, request.Note), ct);
        return Ok(result);
    }

    /// <summary>PUT api/payrolls/pay-all — Chi trả lương toàn bộ nhân sự chưa thanh toán của kỳ (Owner)</summary>
    [HttpPut("pay-all")]
    [Authorize(Roles = "Owner")]
    public async Task<IActionResult> PayAll([FromBody] PayAllPayrollRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new PayAllPayrollCommand(request.Year, request.Month, request.Note), ct);
        return Ok(result);
    }

    /// <summary>PUT api/payrolls/unpay — Hoàn tác chi trả lương của một nhân sự trong kỳ (Owner)</summary>
    [HttpPut("unpay")]
    [Authorize(Roles = "Owner")]
    public async Task<IActionResult> Unpay([FromBody] UnpayPayrollRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new UnpayPayrollCommand(request.Year, request.Month, request.UserId), ct);
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
