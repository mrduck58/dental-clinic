using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DentalClinic.API.Application.UseCases.Appointments;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Presentation.Controllers;

[ApiController]
[Route("api/appointments")]
public class AppointmentsController(
    AppDbContext dbContext,
    CreateAppointmentHandler createAppointmentHandler,
    GetMyAppointmentsHandler getMyAppointmentsHandler,
    GetAllAppointmentsHandler getAllAppointmentsHandler,
    GetWaitingQueueHandler getWaitingQueueHandler,
    GetDentistPatientsHandler getDentistPatientsHandler,
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

    /// <summary>PUT api/appointments/{id}/checkin — Check-in bệnh nhân (Staff/Admin)</summary>
    [HttpPut("{id}/checkin")]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<IActionResult> CheckInAppointment(Guid id, CancellationToken cancellationToken)
    {
        await updateAppointmentStatusHandler.CheckInAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>PUT api/appointments/{id}/start — Bắt đầu khám (Staff/Admin)</summary>
    [HttpPut("{id}/start")]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<IActionResult> StartTreatment(Guid id, CancellationToken cancellationToken)
    {
        await updateAppointmentStatusHandler.StartTreatmentAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>PUT api/appointments/{id}/complete — Hoàn thành khám (Staff/Admin)</summary>
    [HttpPut("{id}/complete")]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<IActionResult> CompleteTreatment(Guid id, CancellationToken cancellationToken)
    {
        await updateAppointmentStatusHandler.CompleteAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>GET api/appointments/queue — Lấy hàng đợi theo bác sĩ (Staff/Admin)</summary>
    [HttpGet("queue")]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<IActionResult> GetWaitingQueue(
        [FromQuery] DateOnly? date,
        CancellationToken cancellationToken)
    {
        var vietnamTz = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        var vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vietnamTz);
        var queryDate = date ?? DateOnly.FromDateTime(vietnamNow);
        var result = await getWaitingQueueHandler.HandleAsync(queryDate, cancellationToken);
        return Ok(result);
    }

    /// <summary>GET api/appointments/dentist/patients — Lấy danh sách bệnh nhân của bác sĩ trong ngày (Dentist)</summary>
    [HttpGet("dentist/patients")]
    [Authorize(Roles = "Dentist")]
    public async Task<IActionResult> GetDentistPatients(
        [FromQuery] DateOnly? date,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var dentist = await dbContext.Dentists.FirstOrDefaultAsync(d => d.UserId == userId, cancellationToken);
        if (dentist == null)
            return NotFound(new { title = "Không tìm thấy thông tin bác sĩ." });

        var vietnamTz = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        var vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vietnamTz);
        var queryDate = date ?? DateOnly.FromDateTime(vietnamNow);

        var result = await getDentistPatientsHandler.HandleAsync(dentist.Id, queryDate, cancellationToken);
        return Ok(result);
    }

    /// <summary>DEBUG: Check appointment status values in database</summary>
    [HttpGet("debug/status")]
    [AllowAnonymous]
    public async Task<IActionResult> DebugStatusValues(CancellationToken cancellationToken)
    {
        var appointments = await dbContext.Appointments
            .Select(a => new { a.Id, Status = (int)a.Status, StatusName = a.Status.ToString() })
            .Take(20)
            .ToListAsync(cancellationToken);
        return Ok(new {
            EnumDefinition = Enum.GetValues<AppointmentStatus>()
                .Select(s => new { Value = (int)s, Name = s.ToString() }),
            Appointments = appointments
        });
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
