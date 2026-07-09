using DentalClinic.API.Application.UseCases.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.API.Presentation.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(Roles = "Admin")]
public class DashboardController(DashboardHandler handler) : ControllerBase
{
    /// <summary>GET api/dashboard/stats — 3 chỉ số chính (bệnh nhân mới, lịch hẹn, doanh thu) kèm % so với kỳ trước.</summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(
        [FromQuery] string range = "week",
        CancellationToken cancellationToken = default)
    {
        var result = await handler.GetStatsAsync(range, cancellationToken);
        return Ok(result);
    }

    /// <summary>GET api/dashboard/appointment-trend — số lượng lịch hẹn theo từng mốc thời gian (biểu đồ cột).</summary>
    [HttpGet("appointment-trend")]
    public async Task<IActionResult> GetAppointmentTrend(
        [FromQuery] string range = "week",
        CancellationToken cancellationToken = default)
    {
        var result = await handler.GetAppointmentTrendAsync(range, cancellationToken);
        return Ok(result);
    }

    /// <summary>GET api/dashboard/service-distribution — tỷ lệ lịch hẹn theo dịch vụ (biểu đồ tròn).</summary>
    [HttpGet("service-distribution")]
    public async Task<IActionResult> GetServiceDistribution(
        [FromQuery] string range = "week",
        [FromQuery] int topN = 5,
        CancellationToken cancellationToken = default)
    {
        var result = await handler.GetServiceDistributionAsync(range, topN, cancellationToken);
        return Ok(result);
    }

    /// <summary>GET api/dashboard/today-appointments — danh sách lịch hẹn hôm nay, có phân trang.</summary>
    [HttpGet("today-appointments")]
    public async Task<IActionResult> GetTodayAppointments(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await handler.GetTodayAppointmentsAsync(page, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>GET api/dashboard/weekly-schedule — lịch vận hành tuần + ca trực bác sĩ của một ngày (mặc định hôm nay).</summary>
    [HttpGet("weekly-schedule")]
    public async Task<IActionResult> GetWeeklySchedule(
        [FromQuery] DateOnly? date,
        CancellationToken cancellationToken = default)
    {
        var result = await handler.GetWeeklyScheduleAsync(date, cancellationToken);
        return Ok(result);
    }

    /// <summary>GET api/dashboard/recent-feedback — đánh giá nổi bật gần đây từ khách hàng + điểm trung bình.</summary>
    [HttpGet("recent-feedback")]
    public async Task<IActionResult> GetRecentFeedback(
        [FromQuery] int limit = 3,
        CancellationToken cancellationToken = default)
    {
        var result = await handler.GetRecentFeedbackAsync(limit, cancellationToken);
        return Ok(result);
    }
}
