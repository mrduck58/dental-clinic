using DentalClinic.API.Application.DTOs.ClinicInfo;
using DentalClinic.API.Application.UseCases.ClinicInfo;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.API.Presentation.Controllers;

[ApiController]
[Route("api/clinic-info")]
public class ClinicInfoController(ISender sender) : ControllerBase
{
    /// <summary>GET api/clinic-info — Thông tin giới thiệu phòng khám (trang chủ / giới thiệu).</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var result = await sender.Send(new GetClinicInfoQuery(), ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>PUT api/clinic-info — Cập nhật thông tin phòng khám (Admin).</summary>
    [HttpPut]
    [Authorize(Roles = "Admin,Owner")]
    public async Task<IActionResult> Update([FromBody] UpdateClinicInfoRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new UpdateClinicInfoCommand(
                request.AboutTitle,
                request.AboutDescription,
                request.FoundedYear,
                request.Phone,
                request.Email,
                request.Address,
                request.AboutImageUrl,
                request.WorkingHours,
                request.Milestones,
                request.Certifications,
                request.Features,
                request.TreatmentSteps,
                request.Statistics),
            ct);
        return Ok(result);
    }
}
