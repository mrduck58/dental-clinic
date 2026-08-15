using DentalClinic.API.Application.DTOs.Commissions;
using DentalClinic.API.Application.UseCases.Commissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.API.Presentation.Controllers;

[ApiController]
[Route("api/commissions")]
[Authorize(Roles = "Owner,Admin")]
public class CommissionsController(ISender sender) : ControllerBase
{
    /// <summary>GET api/commissions — Danh sách quy tắc hoa hồng kèm doanh thu căn cứ + tiền hoa hồng theo kỳ.</summary>
    [HttpGet]
    public async Task<IActionResult> GetRules([FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken ct)
    {
        var result = await sender.Send(new GetCommissionRulesQuery(from, to), ct);
        return Ok(result);
    }

    /// <summary>POST api/commissions — Tạo quy tắc hoa hồng.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CommissionRuleRequest request, CancellationToken ct)
    {
        var id = await sender.Send(new CreateCommissionRuleCommand(request), ct);
        return Ok(new { id });
    }

    /// <summary>PUT api/commissions/{id} — Sửa quy tắc hoa hồng.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CommissionRuleRequest request, CancellationToken ct)
    {
        await sender.Send(new UpdateCommissionRuleCommand(id, request), ct);
        return NoContent();
    }

    /// <summary>PUT api/commissions/{id}/toggle-active — Bật/tắt quy tắc hoa hồng.</summary>
    [HttpPut("{id:guid}/toggle-active")]
    public async Task<IActionResult> ToggleActive(Guid id, CancellationToken ct)
    {
        await sender.Send(new ToggleCommissionRuleActiveCommand(id), ct);
        return NoContent();
    }

    /// <summary>DELETE api/commissions/{id} — Xoá quy tắc hoa hồng.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await sender.Send(new DeleteCommissionRuleCommand(id), ct);
        return NoContent();
    }
}
