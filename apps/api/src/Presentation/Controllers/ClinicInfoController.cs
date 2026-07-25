using DentalClinic.API.Application.DTOs.ClinicInfo;
using DentalClinic.API.Application.UseCases.ClinicInfo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.API.Presentation.Controllers;

[ApiController]
[Route("api/clinic-info")]
public class ClinicInfoController(
    GetClinicInfoHandler get,
    UpdateClinicInfoHandler update) : ControllerBase
{
    /// <summary>GET api/clinic-info — Thông tin giới thiệu phòng khám (trang chủ / giới thiệu).</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var result = await get.HandleAsync(ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>PUT api/clinic-info — Cập nhật thông tin phòng khám (Admin).</summary>
    [HttpPut]
    [Authorize(Roles = "Admin,Owner")]
    public async Task<IActionResult> Update([FromBody] UpdateClinicInfoRequest request, CancellationToken ct)
    {
        var result = await update.HandleAsync(request, ct);
        return Ok(result);
    }
}
