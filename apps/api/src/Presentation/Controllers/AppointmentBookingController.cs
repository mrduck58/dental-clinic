using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DentalClinic.API.Application.UseCases.Booking;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.API.Presentation.Controllers;

/// <summary>
/// Bounded context Booking — đặt lịch / tra lịch hẹn / đổi trạng thái trước khi vào khám.
/// Tách ra từ AppointmentsController (919 dòng, inject 22 handler + AppDbContext).
/// Route được ghi TUYỆT ĐỐI ở từng attribute, y hệt path cũ (3 app frontend đang hardcode).
/// </summary>
[ApiController]
public class AppointmentBookingController(ISender sender) : ControllerBase
{
    /// <summary>POST api/appointments — Đặt lịch khám mới</summary>
    [HttpPost("api/appointments")]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> CreateAppointment(
        [FromBody] CreateAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        Console.WriteLine($"[API] CreateAppointment: Received request for DentistId={request.DentistId}, PatientId={request.PatientId ?? Guid.Empty} (Null if self/not set)");

        var cmd = new CreateAppointmentCommand(
            userId,
            request.DentistId,
            request.AppointmentDate,
            request.Symptoms,
            request.ServiceId,
            request.PatientId);

        var result = await sender.Send(cmd, cancellationToken);
        return Ok(result);
    }

    /// <summary>GET api/appointments/my — Lịch hẹn của bệnh nhân hiện tại</summary>
    [HttpGet("api/appointments/my")]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> GetMyAppointments(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await sender.Send(new GetMyAppointmentsQuery(userId), cancellationToken);
        return Ok(result);
    }

    /// <summary>GET api/appointments — Danh sách tất cả lịch hẹn (Staff/Admin)</summary>
    [HttpGet("api/appointments")]
    [Authorize(Roles = "Staff,Admin,Owner")]
    public async Task<IActionResult> GetAllAppointments(
        [FromQuery] DateOnly? date,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAllAppointmentsQuery(date, status), cancellationToken);
        return Ok(result);
    }

    /// <summary>PUT api/appointments/{id}/confirm — Xác nhận lịch hẹn (Staff/Admin)</summary>
    [HttpPut("api/appointments/{id}/confirm")]
    [Authorize(Roles = "Staff,Admin,Owner")]
    public async Task<IActionResult> ConfirmAppointment(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new ConfirmAppointmentCommand(id), cancellationToken);
        return Ok(new { message = "Đã xác nhận lịch hẹn." });
    }

    /// <summary>PUT api/appointments/{id}/cancel — Hủy lịch hẹn</summary>
    [HttpPut("api/appointments/{id}/cancel")]
    [Authorize(Roles = "Patient,Staff,Admin,Owner")]
    public async Task<IActionResult> CancelAppointment(
        Guid id,
        [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] CancelAppointmentRequest? request,
        CancellationToken cancellationToken)
    {
        var reason = request?.Reason;
        await sender.Send(new CancelAppointmentCommand(id, reason), cancellationToken);
        return Ok(new { message = "Đã hủy lịch hẹn." });
    }

    /// <summary>PUT api/appointments/{id}/checkin — Check-in bệnh nhân (Staff/Admin)</summary>
    [HttpPut("api/appointments/{id}/checkin")]
    [Authorize(Roles = "Staff,Admin,Owner")]
    public async Task<IActionResult> CheckInAppointment(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new CheckInAppointmentCommand(id), cancellationToken);
        return Ok(new { message = "Đã check-in bệnh nhân." });
    }

    /// <summary>PUT api/appointments/{id}/no-show — Ghi nhận bệnh nhân vắng mặt (Staff/Admin)</summary>
    [HttpPut("api/appointments/{id}/no-show")]
    [Authorize(Roles = "Staff,Admin,Owner")]
    public async Task<IActionResult> MarkNoShow(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new MarkNoShowCommand(id), cancellationToken);
        return Ok(new { message = "Đã ghi nhận bệnh nhân vắng mặt." });
    }

    /// <summary>GET api/appointments/staff/schedule — Lịch trống hôm nay cho staff đặt tại quầy</summary>
    [HttpGet("api/appointments/staff/schedule")]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<IActionResult> GetStaffSchedule(
        [FromQuery] DateOnly? date,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetStaffScheduleQuery(date), cancellationToken);
        return Ok(result);
    }

    /// <summary>POST api/appointments/walkin — Đặt lịch tại quầy (Staff/Admin)</summary>
    [HttpPost("api/appointments/walkin")]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<IActionResult> CreateWalkInAppointment(
        [FromBody] CreateWalkInRequest request,
        CancellationToken cancellationToken)
    {
        var cmd = new CreateWalkInCommand(
            request.DentistId,
            request.AppointmentDate,
            request.PatientName,
            request.PatientPhone,
            request.DateOfBirth,
            request.Gender,
            request.ServiceId,
            request.Symptoms,
            request.PatientId);
        var result = await sender.Send(cmd, cancellationToken);
        return Ok(result);
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
    Guid? ServiceId,
    Guid? PatientId);

public record CreateWalkInRequest(
    Guid DentistId,
    DateTimeOffset AppointmentDate,
    string PatientName,
    string PatientPhone,
    DateOnly DateOfBirth,
    string Gender,
    Guid? ServiceId,
    string? Symptoms,
    Guid? PatientId = null);

public record CancelAppointmentRequest(string? Reason);
