using DentalClinic.API.Application.DTOs.Inventory;
using DentalClinic.API.Application.UseCases.Inventory;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.API.Presentation.Controllers;

[ApiController]
[Route("api/inventory")]
[Authorize(Roles = "Admin,Owner,Staff")]
public class InventoryController(ISender sender) : ControllerBase
{
    /// <summary>GET api/inventory/items — Danh sách vật tư</summary>
    [HttpGet("items")]
    public async Task<IActionResult> GetItems(
        [FromQuery] string? search,
        [FromQuery] string? category,
        CancellationToken ct)
    {
        var result = await sender.Send(new GetSupplyItemsQuery(search, category), ct);
        return Ok(result);
    }

    /// <summary>POST api/inventory/items — Thêm vật tư mới</summary>
    [HttpPost("items")]
    public async Task<IActionResult> CreateItem(
        [FromBody] CreateSupplyItemRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(
            new CreateSupplyItemCommand(
                request.Code,
                request.Name,
                request.Category,
                request.Unit,
                request.Quantity,
                request.MinQuantity,
                request.OrderType,
                request.Price),
            ct);
        return Ok(result);
    }

    /// <summary>GET api/inventory/transactions — Lịch sử giao dịch</summary>
    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactions(CancellationToken ct)
    {
        var result = await sender.Send(new GetSupplyTransactionsQuery(), ct);
        return Ok(result);
    }

    /// <summary>POST api/inventory/transactions — Tạo giao dịch nhập/xuất</summary>
    [HttpPost("transactions")]
    public async Task<IActionResult> CreateTransaction(
        [FromBody] CreateSupplyTransactionRequest request,
        CancellationToken ct)
    {
        var createdBy = User.FindFirst("username")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
            ?? "Nhân viên";

        var result = await sender.Send(
            new CreateSupplyTransactionCommand(request.SupplyItemId, request.Type, request.Quantity, request.Note, createdBy),
            ct);
        return Ok(result);
    }

    /// <summary>GET api/inventory/material-requests — Yêu cầu vật tư từ bác sĩ</summary>
    [HttpGet("material-requests")]
    public async Task<IActionResult> GetMaterialRequests([FromQuery] string? status, CancellationToken ct)
    {
        var result = await sender.Send(new GetMaterialRequestsQuery(status), ct);
        return Ok(result);
    }

    /// <summary>POST api/inventory/stock-import — Nhập kho thông minh (tạo mới hoặc cộng vào vật tư đã có)</summary>
    [HttpPost("stock-import")]
    public async Task<IActionResult> StockImport(
        [FromBody] StockImportRequest request,
        CancellationToken ct)
    {
        var createdBy = User.FindFirst("username")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
            ?? "Nhân viên";
        var result = await sender.Send(
            new StockImportCommand(
                request.Name,
                request.Unit,
                request.Category,
                request.Quantity,
                request.Note,
                request.UnitPrice,
                request.OrderType,
                createdBy),
            ct);
        return Ok(result);
    }

    /// <summary>PUT api/inventory/material-requests/{id}/done — Đánh dấu đã xử lý</summary>
    [HttpPut("material-requests/{id}/done")]
    public async Task<IActionResult> MarkMaterialRequestDone(Guid id, CancellationToken ct)
    {
        var handledBy = User.FindFirst("username")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
            ?? "Nhân viên";

        await sender.Send(new MarkMaterialRequestDoneCommand(id, handledBy), ct);
        return Ok(new { message = "Đã đánh dấu hoàn tất yêu cầu vật tư." });
    }
}
