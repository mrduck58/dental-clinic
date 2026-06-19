using DentalClinic.API.Application.UseCases.Staff;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.API.Presentation.Controllers;

[ApiController]
[Route("api/dentists")]
public class DentistsController(GetDentistsHandler getDentistsHandler) : ControllerBase
{
    /// <summary>GET api/dentists — Danh sách nha sĩ công khai dành cho trang chủ mobile</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetDentists(CancellationToken cancellationToken)
    {
        var result = await getDentistsHandler.HandleAsync(cancellationToken);
        return Ok(result);
    }
}
