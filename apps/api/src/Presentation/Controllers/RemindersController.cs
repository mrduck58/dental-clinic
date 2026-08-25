using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DentalClinic.API.Application.UseCases.Reminders;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.API.Presentation.Controllers;

/// <summary>
/// Bounded context Reminders — nhắc tái khám (bác sĩ hẹn ngày; staff check-in bằng cách đặt lịch
/// tại quầy bình thường, xem CreateWalkInAppointmentHandler) và nhắc uống thuốc.
/// Route ghi TUYỆT ĐỐI, y hệt path cũ trong AppointmentsController.
/// </summary>
[ApiController]
public class RemindersController(ISender sender) : ControllerBase
{
    /// <summary>PUT api/appointments/{id}/follow-up-reminder — Hẹn ngày tái khám (Staff/Admin/Dentist)</summary>
    [HttpPut("api/appointments/{id}/follow-up-reminder")]
    [Authorize(Roles = "Staff,Admin,Dentist")]
    public async Task<IActionResult> SetFollowUpReminder(
        Guid id,
        [FromBody] SetFollowUpReminderRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SetFollowUpReminderCommand(id, request), cancellationToken);
        return Ok(result);
    }

    /// <summary>DELETE api/appointments/{id}/follow-up-reminder — Hủy hẹn tái khám (Staff/Admin/Dentist)</summary>
    [HttpDelete("api/appointments/{id}/follow-up-reminder")]
    [Authorize(Roles = "Staff,Admin,Dentist")]
    public async Task<IActionResult> ClearFollowUpReminder(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ClearFollowUpReminderCommand(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>GET api/appointments/follow-up-due — Bệnh nhân đang chờ tái khám (Staff/Admin)</summary>
    [HttpGet("api/appointments/follow-up-due")]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<IActionResult> GetFollowUpDue(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetFollowUpDueQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// GET api/appointments/my/medication-reminders?date=yyyy-MM-dd — Lịch nhắc uống thuốc thật cho một ngày,
    /// sinh từ các đơn thuốc đang trong thời gian sử dụng của bệnh nhân hiện tại và thành viên gia đình.
    /// </summary>
    [HttpGet("api/appointments/my/medication-reminders")]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> GetMyMedicationReminders([FromQuery] DateOnly date, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await sender.Send(new GetMedicationRemindersQuery(userId, date), cancellationToken);
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
