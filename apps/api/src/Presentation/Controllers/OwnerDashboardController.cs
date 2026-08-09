using DentalClinic.API.Application.UseCases.OwnerDashboard;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.API.Presentation.Controllers;

[ApiController]
[Route("api/owner/dashboard")]
[Authorize(Roles = "Owner,Admin")]
public class OwnerDashboardController(ISender sender) : ControllerBase
{
    /// <summary>GET api/owner/dashboard — Báo cáo tổng quan tài chính, tăng trưởng & hiệu suất nhân sự dành riêng cho Owner.</summary>
    [HttpGet]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetOwnerDashboardQuery(), cancellationToken);
        return Ok(result);
    }
}
