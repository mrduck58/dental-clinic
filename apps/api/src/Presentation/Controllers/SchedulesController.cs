using DentalClinic.API.Application.DTOs.Schedules;
using DentalClinic.API.Application.UseCases.Schedules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.API.Presentation.Controllers;

[ApiController]
[Route("api/schedules")]
[Authorize(Roles = "Admin,Owner")]
public class SchedulesController(
    GetWeekScheduleHandler getWeekSchedule,
    SaveWeekScheduleHandler saveWeekSchedule) : ControllerBase
{
    /// <summary>GET api/schedules?weekStart=YYYY-MM-DD — Lấy lịch làm việc theo tuần</summary>
    [HttpGet]
    public async Task<IActionResult> GetByWeek([FromQuery] string weekStart, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(weekStart))
            return BadRequest("weekStart is required (format: YYYY-MM-DD)");

        var result = await getWeekSchedule.HandleAsync(weekStart, ct);
        return Ok(result);
    }

    /// <summary>PUT api/schedules/week/{weekStart} — Lưu toàn bộ lịch làm việc cho một tuần (thay thế)</summary>
    [HttpPut("week/{weekStart}")]
    public async Task<IActionResult> SaveWeek(string weekStart, [FromBody] SaveWeekScheduleRequest request, CancellationToken ct)
    {
        var result = await saveWeekSchedule.HandleAsync(weekStart, request, ct);
        return Ok(result);
    }
}
