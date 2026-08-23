using DentalClinic.API.Application.DTOs.Rooms;
using DentalClinic.API.Application.UseCases.Rooms;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.API.Presentation.Controllers;

[ApiController]
[Route("api/rooms")]
// [Authorize] ở class + method KHÔNG ghi đè nhau — ASP.NET Core cộng dồn (AND) hai điều kiện, nên
// class-level KHÔNG được để Roles hạn chế ở đây, chỉ để [Authorize] (yêu cầu đăng nhập) rồi mỗi action
// tự khai Roles riêng — nếu không Staff sẽ luôn bị 403 dù action GET có khai thêm Staff (xem cùng lý do
// ở InventoryController). Staff cần đọc danh sách phòng cho tab "Xuất kho theo phòng"; quản lý phòng
// (tạo/sửa/xóa/đổi trạng thái) vẫn chỉ dành cho Admin/Owner.
[Authorize]
public class RoomsController(ISender sender) : ControllerBase
{
    /// <summary>GET api/rooms — Danh sách phòng (có lọc theo tầng, trạng thái, tìm kiếm)</summary>
    [HttpGet]
    [Authorize(Roles = "Admin,Owner,Staff")]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? floor,
        [FromQuery] string? status,
        [FromQuery] string? search,
        CancellationToken ct)
    {
        var result = await sender.Send(new GetRoomsQuery(floor, status, search), ct);
        return Ok(result);
    }

    /// <summary>GET api/rooms/{id} — Chi tiết một phòng</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,Owner,Staff")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetRoomByIdQuery(id), ct);
        return Ok(result);
    }

    /// <summary>POST api/rooms — Tạo phòng mới</summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Owner")]
    public async Task<IActionResult> Create([FromBody] CreateRoomRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new CreateRoomCommand(request), ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>PUT api/rooms/{id} — Cập nhật thông tin phòng (tên, mã, tầng, loại, mô tả)</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Owner")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRoomRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new UpdateRoomCommand(id, request), ct);
        return Ok(result);
    }

    /// <summary>DELETE api/rooms/{id} — Xóa phòng</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Owner")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await sender.Send(new DeleteRoomCommand(id), ct);
        return Ok(new { message = "Đã xóa phòng." });
    }

    /// <summary>PATCH api/rooms/{id}/status — Đổi trạng thái phòng</summary>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Admin,Owner")]
    public async Task<IActionResult> ChangeStatus(Guid id, [FromBody] ChangeRoomStatusRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new ChangeRoomStatusCommand(id, request), ct);
        return Ok(result);
    }
}
