using DentalClinic.API.Application.DTOs.Payrolls;
using DentalClinic.API.Application.UseCases.Payrolls;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.API.Presentation.Controllers;

[ApiController]
[Route("api/payrolls")]
[Authorize(Roles = "Owner")]
public class PayrollsController(ISender sender) : ControllerBase
{
    /// <summary>GET api/payrolls?year=&amp;month= — Bảng lương của một kỳ (Owner)</summary>
    [HttpGet]
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
    public async Task<IActionResult> GetYearly([FromQuery] int? year, CancellationToken ct)
    {
        var result = await sender.Send(new GetPayrollYearlyQuery(year ?? DateTime.UtcNow.Year), ct);
        return Ok(result);
    }

    /// <summary>PUT api/payrolls/pay — Chi trả lương một nhân sự trong kỳ (Owner)</summary>
    [HttpPut("pay")]
    public async Task<IActionResult> Pay([FromBody] PayPayrollRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new PayPayrollCommand(request.Year, request.Month, request.UserId, request.Note), ct);
        return Ok(result);
    }

    /// <summary>PUT api/payrolls/pay-all — Chi trả lương toàn bộ nhân sự chưa thanh toán của kỳ (Owner)</summary>
    [HttpPut("pay-all")]
    public async Task<IActionResult> PayAll([FromBody] PayAllPayrollRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new PayAllPayrollCommand(request.Year, request.Month, request.Note), ct);
        return Ok(result);
    }

    /// <summary>PUT api/payrolls/unpay — Hoàn tác chi trả lương của một nhân sự trong kỳ (Owner)</summary>
    [HttpPut("unpay")]
    public async Task<IActionResult> Unpay([FromBody] UnpayPayrollRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new UnpayPayrollCommand(request.Year, request.Month, request.UserId), ct);
        return Ok(result);
    }
}
