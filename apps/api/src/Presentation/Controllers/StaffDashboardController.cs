using DentalClinic.API.Application.UseCases.StaffDashboard;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.API.Presentation.Controllers;

[ApiController]
[Route("api/staff-dashboard")]
[Authorize(Roles = "Staff")]
public class StaffDashboardController(ISender sender) : ControllerBase
{
    /// <summary>GET api/staff-dashboard/stats — 4 chỉ số chính: lịch hẹn hôm nay, chờ check-in, đang khám, chờ thanh toán.</summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetStaffDashboardStatsQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>GET api/staff-dashboard/today-appointments — lịch hẹn hôm nay cần theo dõi (đã xác nhận/check-in/đang khám).</summary>
    [HttpGet("today-appointments")]
    public async Task<IActionResult> GetTodayAppointments(
        [FromQuery] int limit = 5,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetStaffTodayAppointmentsQuery(limit), cancellationToken);
        return Ok(result);
    }

    /// <summary>GET api/staff-dashboard/pending-invoices — hóa đơn chưa thanh toán, cũ nhất trước.</summary>
    [HttpGet("pending-invoices")]
    public async Task<IActionResult> GetPendingInvoices(
        [FromQuery] int limit = 3,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetStaffPendingInvoicesQuery(limit), cancellationToken);
        return Ok(result);
    }
}
