using DentalClinic.API.Application.DTOs.Inventory;
using DentalClinic.API.Application.UseCases.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.API.Presentation.Controllers;

[ApiController]
[Route("api/inventory")]
[Authorize(Roles = "Admin,Owner,Staff")]
public class InventoryController(
    GetSupplyItemsHandler getItems,
    GetSupplyTransactionsHandler getTransactions,
    CreateSupplyItemHandler createItem,
    CreateSupplyTransactionHandler createTransaction) : ControllerBase
{
    /// <summary>GET api/inventory/items — Danh sách vật tư</summary>
    [HttpGet("items")]
    public async Task<IActionResult> GetItems(
        [FromQuery] string? search,
        [FromQuery] string? category,
        CancellationToken ct)
    {
        var result = await getItems.HandleAsync(search, category, ct);
        return Ok(result);
    }

    /// <summary>POST api/inventory/items — Thêm vật tư mới</summary>
    [HttpPost("items")]
    public async Task<IActionResult> CreateItem(
        [FromBody] CreateSupplyItemRequest request,
        CancellationToken ct)
    {
        var result = await createItem.HandleAsync(request, ct);
        return Ok(result);
    }

    /// <summary>GET api/inventory/transactions — Lịch sử giao dịch</summary>
    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactions(CancellationToken ct)
    {
        var result = await getTransactions.HandleAsync(ct);
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

        var result = await createTransaction.HandleAsync(request, createdBy, ct);
        return Ok(result);
    }
}
