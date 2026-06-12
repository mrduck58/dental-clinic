using DentalClinic.API.Application.DTOs.Services;
using DentalClinic.API.Application.UseCases.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.API.Presentation.Controllers;

[ApiController]
[Route("api/services")]
public class ServicesController(
    GetServicesHandler getServices,
    GetServiceByIdHandler getById,
    CreateServiceHandler create,
    UpdateServiceHandler update,
    DeleteServiceHandler delete,
    ToggleServiceStatusHandler toggleStatus) : ControllerBase
{
    /// <summary>GET api/services — Lấy danh sách dịch vụ (có filter)</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? category,
        [FromQuery] string? status,
        [FromQuery] string? search,
        CancellationToken ct)
    {
        var result = await getServices.HandleAsync(category, status, search, ct);
        return Ok(result);
    }

    /// <summary>GET api/services/{id} — Lấy chi tiết một dịch vụ</summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await getById.HandleAsync(id, ct);
        return Ok(result);
    }

    /// <summary>POST api/services — Tạo dịch vụ mới (Admin)</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateServiceRequest request, CancellationToken ct)
    {
        var result = await create.HandleAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>PUT api/services/{id} — Cập nhật dịch vụ (Admin)</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateServiceRequest request, CancellationToken ct)
    {
        var result = await update.HandleAsync(id, request, ct);
        return Ok(result);
    }

    /// <summary>DELETE api/services/{id} — Xóa dịch vụ (Admin)</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await delete.HandleAsync(id, ct);
        return NoContent();
    }

    /// <summary>PATCH api/services/{id}/status — Bật/tắt trạng thái dịch vụ (Admin)</summary>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ToggleStatus(Guid id, CancellationToken ct)
    {
        var result = await toggleStatus.HandleAsync(id, ct);
        return Ok(result);
    }
}
