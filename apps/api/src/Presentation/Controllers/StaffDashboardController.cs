using DentalClinic.API.Application.UseCases.StaffDashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.API.Presentation.Controllers;

[ApiController]
[Route("api/staff-dashboard")]
[Authorize(Roles = "Staff")]
public class StaffDashboardController(StaffDashboardHandler handler) : ControllerBase
{
    /// <summary>GET api/staff-dashboard/stats — 4 chỉ số chính: lịch hẹn hôm nay, chờ check-in, đang khám, chờ thanh toán.</summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken cancellationToken)
    {
        var result = await handler.GetStatsAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>GET api/staff-dashboard/today-appointments — lịch hẹn hôm nay cần theo dõi (đã xác nhận/check-in/đang khám).</summary>
    [HttpGet("today-appointments")]
    public async Task<IActionResult> GetTodayAppointments(
        [FromQuery] int limit = 5,
        CancellationToken cancellationToken = default)
    {
        var result = await handler.GetTodayAppointmentsAsync(limit, cancellationToken);
        return Ok(result);
    }

    /// <summary>GET api/staff-dashboard/pending-invoices — hóa đơn chưa thanh toán, cũ nhất trước.</summary>
    [HttpGet("pending-invoices")]
    public async Task<IActionResult> GetPendingInvoices(
        [FromQuery] int limit = 3,
        CancellationToken cancellationToken = default)
    {
        var result = await handler.GetPendingInvoicesAsync(limit, cancellationToken);
        return Ok(result);
    }
}
