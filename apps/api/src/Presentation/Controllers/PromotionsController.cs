using DentalClinic.API.Application.DTOs.Promotions;
using DentalClinic.API.Application.UseCases.Promotions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.API.Presentation.Controllers;

[ApiController]
[Route("api/promotions")]
public class PromotionsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(await sender.Send(new GetPromotionsQuery(), ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var dto = await sender.Send(new GetPromotionByIdQuery(id), ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreatePromotionRequest req, CancellationToken ct)
    {
        var id = await sender.Send(
            new CreatePromotionCommand(
                req.Code, req.Name, req.Description,
                req.DiscountType, req.DiscountValue,
                req.ServiceIds, req.StartDate, req.EndDate,
                req.IsActive),
            ct);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePromotionRequest req, CancellationToken ct)
    {
        var ok = await sender.Send(
            new UpdatePromotionCommand(
                id, req.Code, req.Name, req.Description,
                req.DiscountType, req.DiscountValue,
                req.ServiceIds, req.StartDate, req.EndDate),
            ct);
        return ok ? Ok(new { message = "Đã cập nhật khuyến mãi." }) : NotFound();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var ok = await sender.Send(new DeletePromotionCommand(id), ct);
        return ok ? Ok(new { message = "Đã xóa khuyến mãi." }) : NotFound();
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ToggleStatus(Guid id, CancellationToken ct)
    {
        var dto = await sender.Send(new TogglePromotionStatusCommand(id), ct);
        return dto is null ? NotFound() : Ok(dto);
    }
}
