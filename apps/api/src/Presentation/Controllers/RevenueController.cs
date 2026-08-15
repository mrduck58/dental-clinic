using DentalClinic.API.Application.UseCases.Revenue;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.API.Presentation.Controllers;

[ApiController]
[Route("api/revenue")]
[Authorize(Roles = "Owner,Admin")]
public class RevenueController(ISender sender) : ControllerBase
{
    /// <summary>GET api/revenue/summary — Tổng/Đã thu/Chưa thu/Hoàn tiền trong khoảng ngày.</summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken ct)
    {
        var result = await sender.Send(new GetRevenueSummaryQuery(from, to), ct);
        return Ok(result);
    }

    /// <summary>GET api/revenue/transactions — Danh sách giao dịch doanh thu, lọc nhiều chiều + phân trang.</summary>
    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactions(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromQuery] Guid? dentistId,
        [FromQuery] string? serviceName,
        [FromQuery] string? status,
        [FromQuery] string? paymentMethod,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDir = null,
        CancellationToken ct = default)
    {
        var result = await sender.Send(
            new GetRevenueTransactionsPagedQuery(
                from, to, dentistId, serviceName, status, paymentMethod, search, page, pageSize, sortBy, sortDir),
            ct);
        return Ok(result);
    }

    /// <summary>GET api/revenue/charts — Doanh thu đã thu, nhóm theo dịch vụ và theo bác sĩ (top 10).</summary>
    [HttpGet("charts")]
    public async Task<IActionResult> GetCharts([FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken ct)
    {
        var result = await sender.Send(new GetRevenueChartsQuery(from, to), ct);
        return Ok(result);
    }
}
