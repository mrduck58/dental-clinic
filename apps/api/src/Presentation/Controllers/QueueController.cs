using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DentalClinic.API.Application.UseCases.Queue;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.API.Presentation.Controllers;

/// <summary>
/// Bounded context Queue — hàng đợi khám trong ngày (lễ tân và bệnh nhân).
/// Route ghi TUYỆT ĐỐI, y hệt path cũ trong AppointmentsController.
/// </summary>
[ApiController]
public class QueueController(ISender sender) : ControllerBase
{
    /// <summary>GET api/appointments/queue — Lấy hàng đợi theo bác sĩ (Staff/Admin)</summary>
    [HttpGet("api/appointments/queue")]
    [Authorize(Roles = "Staff,Admin,Owner")]
    public async Task<IActionResult> GetWaitingQueue(
        [FromQuery] DateOnly? date,
        CancellationToken cancellationToken)
    {
        var vietnamTz = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        var vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vietnamTz);
        var queryDate = date ?? DateOnly.FromDateTime(vietnamNow);
        var result = await sender.Send(new GetWaitingQueueQuery(queryDate), cancellationToken);
        return Ok(result);
    }

    /// <summary>GET api/appointments/queue/patient — Lấy hàng đợi của bệnh nhân/thành viên (Patient)</summary>
    [HttpGet("api/appointments/queue/patient")]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> GetPatientQueue(
        [FromQuery] Guid? patientId,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await sender.Send(new GetPatientQueueQuery(userId, patientId), cancellationToken);
        return Ok(result);
    }

    /// <summary>PUT api/appointments/{id}/queue-room — Chuyển bệnh nhân đang chờ sang hàng đợi phòng khác (Staff/Admin)</summary>
    [HttpPut("api/appointments/{id}/queue-room")]
    [Authorize(Roles = "Staff,Admin,Owner")]
    public async Task<IActionResult> TransferQueuePatient(
        Guid id,
        [FromBody] TransferQueuePatientRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new TransferQueuePatientCommand(id, request.RoomName, request.DentistId), cancellationToken);
        return Ok(result);
    }

    /// <summary>PUT api/appointments/{id}/queue-order — Đổi chỗ thứ tự với bệnh nhân liền kề trong hàng đợi (Staff/Admin)</summary>
    [HttpPut("api/appointments/{id}/queue-order")]
    [Authorize(Roles = "Staff,Admin,Owner")]
    public async Task<IActionResult> ReorderQueuePatient(
        Guid id,
        [FromBody] ReorderQueuePatientRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new ReorderQueuePatientCommand(id, request.SwapWithAppointmentId), cancellationToken);
        return Ok(new { message = "Đã đổi thứ tự hàng đợi." });
    }

    private Guid GetCurrentUserId()
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("Không xác định được người dùng từ token.");
        return Guid.Parse(sub);
    }
}

public record TransferQueuePatientRequest(string RoomName, Guid? DentistId = null);

public record ReorderQueuePatientRequest(Guid SwapWithAppointmentId);
