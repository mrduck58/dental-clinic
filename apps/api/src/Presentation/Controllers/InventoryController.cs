using DentalClinic.API.Application.DTOs.Inventory;
using DentalClinic.API.Application.UseCases.Inventory;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.API.Presentation.Controllers;

[ApiController]
[Route("api/inventory")]
[Authorize]
public class InventoryController(ISender sender) : ControllerBase
{
    /// <summary>GET api/inventory/items — Danh sách vật tư (Dentist chỉ dùng để autocomplete khi tạo yêu cầu vật tư)</summary>
    // Lưu ý: [Authorize] ở class + method KHÔNG ghi đè nhau — ASP.NET Core cộng dồn (AND) hai điều kiện,
    // nên class-level KHÔNG được để Roles hạn chế ở đây, chỉ để [Authorize] (yêu cầu đăng nhập) rồi mỗi
    // action tự khai Roles riêng — nếu không Dentist sẽ luôn bị 403 dù action này có khai thêm Dentist.
    [HttpGet("items")]
    [Authorize(Roles = "Admin,Owner,Staff,Dentist")]
    public async Task<IActionResult> GetItems(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] string? orderType,
        CancellationToken ct)
    {
        var result = await sender.Send(new GetSupplyItemsQuery(search, category, orderType), ct);
        return Ok(result);
    }

    /// <summary>POST api/inventory/items — Thêm vật tư mới</summary>
    [HttpPost("items")]
    [Authorize(Roles = "Admin,Owner,Staff")]
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
                request.Price),
            ct);
        return Ok(result);
    }

    /// <summary>PUT api/inventory/items/{id} — Sửa thông tin vật tư (tên, danh mục, đơn vị, tồn tối thiểu, giá)</summary>
    [HttpPut("items/{id:guid}")]
    [Authorize(Roles = "Admin,Owner,Staff")]
    public async Task<IActionResult> UpdateItem(Guid id, [FromBody] UpdateSupplyItemRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new UpdateSupplyItemCommand(id, request.Name, request.Category, request.Unit, request.MinQuantity, request.Price),
            ct);
        return Ok(result);
    }

    /// <summary>DELETE api/inventory/items/{id} — Xóa vật tư (chỉ khi chưa có giao dịch/định mức liên quan)</summary>
    [HttpDelete("items/{id:guid}")]
    [Authorize(Roles = "Admin,Owner,Staff")]
    public async Task<IActionResult> DeleteItem(Guid id, CancellationToken ct)
    {
        await sender.Send(new DeleteSupplyItemCommand(id), ct);
        return Ok(new { message = "Đã xóa vật tư." });
    }

    /// <summary>GET api/inventory/transactions — Lịch sử giao dịch (roomId → chỉ lấy giao dịch xuất cho
    /// đúng phòng đó, dùng ở màn chi tiết phòng bên Admin)</summary>
    [HttpGet("transactions")]
    [Authorize(Roles = "Admin,Owner,Staff")]
    public async Task<IActionResult> GetTransactions([FromQuery] Guid? roomId, CancellationToken ct)
    {
        var result = await sender.Send(new GetSupplyTransactionsQuery(roomId), ct);
        return Ok(result);
    }

    /// <summary>POST api/inventory/transactions — Tạo giao dịch nhập/xuất</summary>
    [HttpPost("transactions")]
    [Authorize(Roles = "Admin,Owner,Staff")]
    public async Task<IActionResult> CreateTransaction(
        [FromBody] CreateSupplyTransactionRequest request,
        CancellationToken ct)
    {
        var createdBy = User.FindFirst("username")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
            ?? "Nhân viên";

        var result = await sender.Send(
            new CreateSupplyTransactionCommand(request.SupplyItemId, request.Type, request.Quantity, request.Note, createdBy, request.RoomId),
            ct);
        return Ok(result);
    }

    /// <summary>GET api/inventory/material-requests — Yêu cầu vật tư từ bác sĩ</summary>
    [HttpGet("material-requests")]
    [Authorize(Roles = "Admin,Owner,Staff")]
    public async Task<IActionResult> GetMaterialRequests([FromQuery] string? status, CancellationToken ct)
    {
        var result = await sender.Send(new GetMaterialRequestsQuery(status), ct);
        return Ok(result);
    }

    /// <summary>POST api/inventory/material-requests/staff — Staff tự khởi tạo yêu cầu đặt vật tư riêng cho bệnh nhân</summary>
    [HttpPost("material-requests/staff")]
    [Authorize(Roles = "Admin,Owner,Staff")]
    public async Task<IActionResult> CreateMaterialRequestByStaff(
        [FromBody] CreateMaterialRequestByStaffRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return Ok(result);
    }

    /// <summary>PUT api/inventory/material-requests/{id}/ordered — Đánh dấu đã đặt hàng nhà cung cấp/lab (chưa nhập kho)</summary>
    [HttpPut("material-requests/{id}/ordered")]
    [Authorize(Roles = "Admin,Owner,Staff")]
    public async Task<IActionResult> MarkMaterialRequestOrdered(Guid id, [FromBody] MarkMaterialRequestOrderedRequest request, CancellationToken ct)
    {
        var orderedBy = User.FindFirst("username")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
            ?? "Nhân viên";

        var result = await sender.Send(new MarkMaterialRequestOrderedCommand(id, orderedBy, request.SupplierNote), ct);
        return Ok(result);
    }

    /// <summary>POST api/inventory/stock-import — Nhập kho thông minh (tạo mới hoặc cộng vào vật tư đã có)</summary>
    [HttpPost("stock-import")]
    [Authorize(Roles = "Admin,Owner,Staff")]
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
                createdBy),
            ct);
        return Ok(result);
    }

    /// <summary>PUT api/inventory/material-requests/{id}/done — Nhập kho từng vật tư (kèm giá) rồi đánh dấu đã xử lý</summary>
    [HttpPut("material-requests/{id}/done")]
    [Authorize(Roles = "Admin,Owner,Staff")]
    public async Task<IActionResult> MarkMaterialRequestDone(Guid id, [FromBody] MarkMaterialRequestDoneRequest request, CancellationToken ct)
    {
        var handledBy = User.FindFirst("username")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
            ?? "Nhân viên";

        await sender.Send(new MarkMaterialRequestDoneCommand(id, handledBy, request.ItemPrices), ct);
        return Ok(new { message = "Đã đánh dấu hoàn tất yêu cầu vật tư." });
    }
}

public record MarkMaterialRequestDoneRequest(List<MaterialRequestItemPriceInput> ItemPrices);

public record MarkMaterialRequestOrderedRequest(string? SupplierNote);
