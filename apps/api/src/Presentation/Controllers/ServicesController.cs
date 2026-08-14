using DentalClinic.API.Application.DTOs.Services;
using DentalClinic.API.Application.UseCases.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.API.Presentation.Controllers;

[ApiController]
[Route("api/services")]
public class ServicesController(ISender sender) : ControllerBase
{
    /// <summary>GET api/services — Lấy danh sách dịch vụ (có filter)</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        [FromQuery] string? search,
        CancellationToken ct)
    {
        var result = await sender.Send(new GetServicesQuery(status, search), ct);
        return Ok(result);
    }

    /// <summary>GET api/services/{id} — Lấy chi tiết một dịch vụ</summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetServiceByIdQuery(id), ct);
        return Ok(result);
    }

    /// <summary>POST api/services — Tạo dịch vụ mới (Admin)</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateServiceRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new CreateServiceCommand(
                request.Name, request.Price, request.DurationMinutes,
                request.Description, request.Content ?? string.Empty,
                request.ImageUrl, request.IconUrl, request.Options),
            ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>PUT api/services/{id} — Cập nhật dịch vụ (Admin)</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateServiceRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new UpdateServiceCommand(
                id, request.Name, request.Price, request.DurationMinutes,
                request.Description, request.Content ?? string.Empty,
                request.ImageUrl, request.IconUrl, request.Options),
            ct);
        return Ok(result);
    }

    /// <summary>DELETE api/services/{id} — Xóa dịch vụ (Admin)</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await sender.Send(new DeleteServiceCommand(id), ct);
        return Ok(new { message = "Đã xóa dịch vụ." });
    }

    /// <summary>PATCH api/services/{id}/status — Bật/tắt trạng thái dịch vụ (Admin)</summary>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ToggleStatus(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new ToggleServiceStatusCommand(id), ct);
        return Ok(result);
    }

    /// <summary>GET api/services/{id}/procedures — Quy trình điều trị chuẩn của dịch vụ</summary>
    [HttpGet("{id:guid}/procedures")]
    [Authorize(Roles = "Admin,Dentist,Staff,Owner")]
    public async Task<IActionResult> GetProcedures(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetTreatmentProceduresQuery(id), ct);
        return Ok(result);
    }

    /// <summary>PUT api/services/{id}/procedures — Thay toàn bộ quy trình điều trị của dịch vụ (Admin)</summary>
    [HttpPut("{id:guid}/procedures")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ReplaceProcedures(
        Guid id,
        [FromBody] List<ProcedureStepRequest> steps,
        CancellationToken ct)
    {
        var result = await sender.Send(new ReplaceTreatmentProceduresCommand(id, steps), ct);
        return Ok(result);
    }
}
