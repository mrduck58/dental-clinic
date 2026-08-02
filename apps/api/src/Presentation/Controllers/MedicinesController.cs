using DentalClinic.API.Application.DTOs.Medicines;
using DentalClinic.API.Application.UseCases.Medicines;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.API.Presentation.Controllers;

[ApiController]
[Route("api/medicines")]
public class MedicinesController(ISender sender) : ControllerBase
{
    /// <summary>GET api/medicines — Lấy danh sách thuốc</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        CancellationToken ct)
    {
        var result = await sender.Send(new GetMedicinesQuery(search), ct);
        return Ok(result);
    }

    /// <summary>GET api/medicines/{id} — Lấy chi tiết một thuốc</summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetMedicineByIdQuery(id), ct);
        return Ok(result);
    }

    /// <summary>POST api/medicines — Tạo thuốc mới (Admin)</summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Owner")]
    public async Task<IActionResult> Create([FromBody] CreateMedicineRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new CreateMedicineCommand(request.Name, request.GenericName, request.Manufacturer, request.Unit, request.Description),
            ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>PUT api/medicines/{id} — Cập nhật thuốc (Admin)</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Owner")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateMedicineRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new UpdateMedicineCommand(id, request.Name, request.GenericName, request.Manufacturer, request.Unit, request.Description),
            ct);
        return Ok(result);
    }

    /// <summary>DELETE api/medicines/{id} — Xóa thuốc (Admin)</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Owner")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await sender.Send(new DeleteMedicineCommand(id), ct);
        return Ok(new { message = "Đã xóa thuốc." });
    }
}
