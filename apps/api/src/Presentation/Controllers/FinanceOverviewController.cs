using DentalClinic.API.Application.UseCases.Finance;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.API.Presentation.Controllers;

[ApiController]
[Route("api/finance/overview")]
[Authorize(Roles = "Owner,Admin")]
public class FinanceOverviewController(ISender sender) : ControllerBase
{
    /// <summary>GET api/finance/overview — Tổng quan tài chính: doanh thu/chi phí/lương/lợi nhuận + top dịch vụ/bác sĩ + giao dịch gần đây.</summary>
    [HttpGet]
    public async Task<IActionResult> GetOverview([FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken ct)
    {
        var result = await sender.Send(new GetFinanceOverviewQuery(from, to), ct);
        return Ok(result);
    }
}
