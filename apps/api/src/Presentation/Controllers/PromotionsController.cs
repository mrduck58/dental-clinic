using DentalClinic.API.Application.DTOs.Promotions;
using DentalClinic.API.Application.UseCases.Promotions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.API.Presentation.Controllers;

[ApiController]
[Route("api/promotions")]
public class PromotionsController(
    GetPromotionsHandler getAll,
    GetPromotionByIdHandler getById,
    CreatePromotionHandler create,
    UpdatePromotionHandler update,
    DeletePromotionHandler delete,
    TogglePromotionStatusHandler toggle) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(await getAll.HandleAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var dto = await getById.HandleAsync(id, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreatePromotionRequest req, CancellationToken ct)
    {
        var id = await create.HandleAsync(req, ct);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePromotionRequest req, CancellationToken ct)
    {
        var ok = await update.HandleAsync(id, req, ct);
        return ok ? NoContent() : NotFound();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var ok = await delete.HandleAsync(id, ct);
        return ok ? NoContent() : NotFound();
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ToggleStatus(Guid id, CancellationToken ct)
    {
        var dto = await toggle.HandleAsync(id, ct);
        return dto is null ? NotFound() : Ok(dto);
    }
}
