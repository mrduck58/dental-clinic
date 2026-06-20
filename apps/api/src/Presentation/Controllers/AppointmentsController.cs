using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DentalClinic.API.Application.UseCases.Appointments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.API.Presentation.Controllers;

[ApiController]
[Route("api/appointments")]
public class AppointmentsController(
    CreateAppointmentHandler createAppointmentHandler,
    GetMyAppointmentsHandler getMyAppointmentsHandler,
    GetAllAppointmentsHandler getAllAppointmentsHandler,
    UpdateAppointmentStatusHandler updateAppointmentStatusHandler) : ControllerBase
{
    /// <summary>POST api/appointments — Đặt lịch khám mới</summary>
    [HttpPost]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> CreateAppointment(
        [FromBody] CreateAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var cmd = new CreateAppointmentCommand(
            userId,
            request.DentistId,
            request.AppointmentDate,
            request.Symptoms,
            request.ServiceId);

        var result = await createAppointmentHandler.HandleAsync(cmd, cancellationToken);
        return Ok(result);
    }

    /// <summary>GET api/appointments/my — Lịch hẹn của bệnh nhân hiện tại</summary>
    [HttpGet("my")]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> GetMyAppointments(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await getMyAppointmentsHandler.HandleAsync(userId, cancellationToken);
        return Ok(result);
    }

    /// <summary>GET api/appointments — Danh sách tất cả lịch hẹn (Staff/Admin)</summary>
    [HttpGet]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<IActionResult> GetAllAppointments(
        [FromQuery] DateOnly? date,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var result = await getAllAppointmentsHandler.HandleAsync(date, status, cancellationToken);
        return Ok(result);
    }

    /// <summary>PUT api/appointments/{id}/confirm — Xác nhận lịch hẹn (Staff/Admin)</summary>
    [HttpPut("{id}/confirm")]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<IActionResult> ConfirmAppointment(Guid id, CancellationToken cancellationToken)
    {
        await updateAppointmentStatusHandler.ConfirmAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>PUT api/appointments/{id}/cancel — Hủy lịch hẹn (Staff/Admin)</summary>
    [HttpPut("{id}/cancel")]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<IActionResult> CancelAppointment(Guid id, CancellationToken cancellationToken)
    {
        await updateAppointmentStatusHandler.CancelAsync(id, cancellationToken);
        return NoContent();
    }

    private Guid GetCurrentUserId()
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("Không xác định được người dùng từ token.");
        return Guid.Parse(sub);
    }
}

public record CreateAppointmentRequest(
    Guid DentistId,
    DateTimeOffset AppointmentDate,
    string? Symptoms,
    Guid? ServiceId);
