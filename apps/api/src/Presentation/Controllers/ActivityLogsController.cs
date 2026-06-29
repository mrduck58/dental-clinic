using DentalClinic.API.Application.UseCases.ActivityLogs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.API.Presentation.Controllers;

[ApiController]
[Route("api/activity-logs")]
[Authorize(Roles = "Admin,Owner")]
public class ActivityLogsController(GetActivityLogsHandler getActivityLogsHandler) : ControllerBase
{
    /// <summary>GET api/activity-logs — Lấy danh sách activity log có filter và phân trang</summary>
    [HttpGet]
    public async Task<IActionResult> GetActivityLogs(
        [FromQuery] string? action,
        [FromQuery] string? module,
        [FromQuery] string? status,
        [FromQuery] string? search,
        [FromQuery] DateTimeOffset? startDate,
        [FromQuery] DateTimeOffset? endDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await getActivityLogsHandler.HandleAsync(
            new GetActivityLogsQuery(action, module, status, search, startDate, endDate, page, pageSize),
            cancellationToken);

        return Ok(result);
    }
}
