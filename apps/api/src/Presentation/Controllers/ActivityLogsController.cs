using DentalClinic.API.Application.UseCases.ActivityLogs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.API.Presentation.Controllers;

[ApiController]
[Route("api/activity-logs")]
[Authorize(Roles = "Admin,Owner,Dentist,Staff")]
public class ActivityLogsController(ISender sender) : ControllerBase
{
    /// <summary>GET api/activity-logs — Lấy danh sách activity log có filter và phân trang</summary>
    [HttpGet]
    public async Task<IActionResult> GetActivityLogs(
        [FromQuery] Guid? userId,
        [FromQuery] string? action,
        [FromQuery] string? module,
        [FromQuery] string? status,
        [FromQuery] string? search,
        [FromQuery] DateTimeOffset? startDate,
        [FromQuery] DateTimeOffset? endDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortDir = null,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new GetActivityLogsQuery(userId, action, module, status, search, startDate, endDate, page, pageSize, sortDir),
            cancellationToken);

        return Ok(result);
    }
}
