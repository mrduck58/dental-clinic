using DentalClinic.API.Application.DTOs.Medicines;
using DentalClinic.API.Application.UseCases.Medicines;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.API.Presentation.Controllers;

[ApiController]
[Route("api/medicines")]
public class MedicinesController(
    GetMedicinesHandler getMedicines,
    GetMedicineByIdHandler getById,
    CreateMedicineHandler create,
    UpdateMedicineHandler update,
    DeleteMedicineHandler delete) : ControllerBase
{
    /// <summary>GET api/medicines — Lấy danh sách thuốc</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        CancellationToken ct)
    {
        var result = await getMedicines.HandleAsync(search, ct);
        return Ok(result);
    }

    /// <summary>GET api/medicines/{id} — Lấy chi tiết một thuốc</summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await getById.HandleAsync(id, ct);
        return Ok(result);
    }

    /// <summary>POST api/medicines — Tạo thuốc mới (Admin)</summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Owner")]
    public async Task<IActionResult> Create([FromBody] CreateMedicineRequest request, CancellationToken ct)
    {
        var result = await create.HandleAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>PUT api/medicines/{id} — Cập nhật thuốc (Admin)</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Owner")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateMedicineRequest request, CancellationToken ct)
    {
        var result = await update.HandleAsync(id, request, ct);
        return Ok(result);
    }

    /// <summary>DELETE api/medicines/{id} — Xóa thuốc (Admin)</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Owner")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await delete.HandleAsync(id, ct);
        return Ok(new { message = "Đã xóa thuốc." });
    }
}
