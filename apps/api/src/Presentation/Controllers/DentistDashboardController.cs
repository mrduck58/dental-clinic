using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DentalClinic.API.Application.UseCases.DentistDashboard;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.API.Presentation.Controllers;

/// <summary>
/// Bounded context DentistDashboard — màn hình tổng quan và danh sách bệnh nhân của bác sĩ.
/// Route ghi TUYỆT ĐỐI, y hệt path cũ trong AppointmentsController.
/// </summary>
[ApiController]
public class DentistDashboardController(ISender sender) : ControllerBase
{
    /// <summary>GET api/appointments/dentist/dashboard — Dữ liệu tổng quan cho bác sĩ (Dentist/Admin/Owner)</summary>
    [HttpGet("api/appointments/dentist/dashboard")]
    [Authorize(Roles = "Dentist,Admin,Owner")]
    public async Task<IActionResult> GetDentistDashboard(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await sender.Send(new GetDentistDashboardQuery(userId), cancellationToken);
        return Ok(result);
    }

    /// <summary>GET api/appointments/dentist/patients — Lấy danh sách bệnh nhân của bác sĩ trong ngày (Dentist/Admin/Owner)</summary>
    [HttpGet("api/appointments/dentist/patients")]
    [Authorize(Roles = "Dentist,Admin,Owner")]
    public async Task<IActionResult> GetDentistPatients(
        [FromQuery] DateOnly? date,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await sender.Send(new GetMyDentistPatientsQuery(userId, date), cancellationToken);
        return Ok(result ?? new DentistPatientsResponse(date ?? DateOnly.FromDateTime(DateTime.UtcNow), 0, 0, 0, []));
    }

    /// <summary>GET api/appointments/dentist/patients/past — Lấy danh sách bệnh nhân đã từng khám của bác sĩ (Dentist/Admin/Owner)</summary>
    [HttpGet("api/appointments/dentist/patients/past")]
    [Authorize(Roles = "Dentist,Admin,Owner")]
    public async Task<IActionResult> GetDentistPastPatients(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var patients = await sender.Send(new GetDentistPastPatientsQuery(userId), cancellationToken);
        return Ok(patients ?? new List<DentistPatientDto>());
    }

    private Guid GetCurrentUserId()
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("Không xác định được người dùng từ token.");
        return Guid.Parse(sub);
    }
}
