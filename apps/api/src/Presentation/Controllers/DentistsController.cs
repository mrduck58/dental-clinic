using DentalClinic.API.Application.UseCases.Appointments;
using DentalClinic.API.Application.UseCases.Staff;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.API.Presentation.Controllers;

[ApiController]
[Route("api/dentists")]
public class DentistsController(
    GetDentistsHandler getDentistsHandler,
    GetDentistSlotsHandler getDentistSlotsHandler) : ControllerBase
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
}
