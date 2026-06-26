using DentalClinic.API.Application.UseCases.Appointments;
using DentalClinic.API.Application.UseCases.Staff;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.API.Presentation.Controllers;

[ApiController]
[Route("api/dentists")]
public class DentistsController(
    GetDentistsHandler getDentistsHandler,
    GetDentistSlotsHandler getDentistSlotsHandler,
    GetFollowUpSlotsHandler getFollowUpSlotsHandler) : ControllerBase
{
    /// <summary>GET api/dentists — Danh sách nha sĩ cho trang chủ mobile</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetDentists(CancellationToken cancellationToken)
    {
        var result = await getDentistsHandler.HandleAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>GET api/dentists/slots?date=2026-06-20 — Nha sĩ kèm slot khả dụng cho ngày đã chọn</summary>
    [HttpGet("slots")]
    [AllowAnonymous]
    public async Task<IActionResult> GetDentistSlots(
        [FromQuery] DateOnly date,
        CancellationToken cancellationToken)
    {
        var result = await getDentistSlotsHandler.HandleAsync(date, cancellationToken);
        return Ok(result);
    }

    /// <summary>GET api/dentists/{dentistId}/slots?date=2026-06-20 — Lấy slots cho bác sĩ cụ thể (tái khám)</summary>
    [HttpGet("{dentistId}/slots")]
    [Authorize(Roles = "Dentist,Admin,Staff")]
    public async Task<IActionResult> GetFollowUpSlots(
        Guid dentistId,
        [FromQuery] DateOnly date,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"[CONTROLLER] GetFollowUpSlots called: dentistId={dentistId}, date={date}");
        var result = await getFollowUpSlotsHandler.HandleAsync(dentistId, date, cancellationToken);
        Console.WriteLine($"[CONTROLLER] Result: HasSchedule={result.HasSchedule}, Message={result.Message}, Slots={result.Slots.Count}");
        return Ok(result);
    }
}
