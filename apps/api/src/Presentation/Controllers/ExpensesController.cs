using DentalClinic.API.Application.DTOs.Expenses;
using DentalClinic.API.Application.UseCases.Expenses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.API.Presentation.Controllers;

[ApiController]
[Route("api/expenses")]
[Authorize(Roles = "Owner,Admin")]
public class ExpensesController(ISender sender) : ControllerBase
{
    /// <summary>GET api/expenses — Danh sách chi phí tự nhập, lọc theo danh mục/khoảng ngày/tìm kiếm + phân trang.</summary>
    [HttpGet]
    public async Task<IActionResult> GetExpenses(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromQuery] string? category,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDir = null,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new GetExpensesPagedQuery(from, to, category, search, page, pageSize, sortBy, sortDir), ct);
        return Ok(result);
    }

    /// <summary>GET api/expenses/summary — Tổng chi phí trong kỳ (gồm cả vật tư và lương).</summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken ct)
    {
        var result = await sender.Send(new GetExpenseSummaryQuery(from, to), ct);
        return Ok(result);
    }

    /// <summary>GET api/expenses/charts — Chi phí nhóm theo danh mục (gồm cả Vật tư, Lương) trong kỳ.</summary>
    [HttpGet("charts")]
    public async Task<IActionResult> GetCharts([FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken ct)
    {
        var result = await sender.Send(new GetExpenseChartsQuery(from, to), ct);
        return Ok(result);
    }

    /// <summary>POST api/expenses — Thêm khoản chi phí mới.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateExpenseRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new CreateExpenseCommand(request), ct);
        return Ok(result);
    }

    /// <summary>PUT api/expenses/{id} — Sửa khoản chi phí.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateExpenseRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new UpdateExpenseCommand(id, request), ct);
        return Ok(result);
    }

    /// <summary>DELETE api/expenses/{id} — Xoá khoản chi phí.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await sender.Send(new DeleteExpenseCommand(id), ct);
        return NoContent();
    }

    /// <summary>POST api/expenses/generate-recurring — Sinh chi phí định kỳ cho kỳ hiện tại từ các mẫu đang hoạt động.</summary>
    [HttpPost("generate-recurring")]
    public async Task<IActionResult> GenerateRecurring(CancellationToken ct)
    {
        var todayVn = DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")).DateTime);
        var result = await sender.Send(new GenerateRecurringExpensesCommand(todayVn), ct);
        return Ok(result);
    }
}
