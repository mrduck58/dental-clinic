using DentalClinic.API.Application.DTOs.Rooms;
using DentalClinic.API.Application.UseCases.Rooms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.API.Presentation.Controllers;

[ApiController]
[Route("api/rooms")]
[Authorize(Roles = "Admin")]
public class RoomsController(
    GetRoomsHandler getRooms,
    GetRoomByIdHandler getById,
    CreateRoomHandler create,
    UpdateRoomHandler update,
    DeleteRoomHandler delete,
    ChangeRoomStatusHandler changeStatus) : ControllerBase
{
    /// <summary>GET api/rooms — Danh sách phòng (có lọc theo tầng, trạng thái, tìm kiếm)</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? floor,
        [FromQuery] string? status,
        [FromQuery] string? search,
        CancellationToken ct)
    {
        var result = await getRooms.HandleAsync(floor, status, search, ct);
        return Ok(result);
    }

    /// <summary>GET api/rooms/{id} — Chi tiết một phòng</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await getById.HandleAsync(id, ct);
        return Ok(result);
    }

    /// <summary>POST api/rooms — Tạo phòng mới</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRoomRequest request, CancellationToken ct)
    {
        var result = await create.HandleAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>PUT api/rooms/{id} — Cập nhật thông tin phòng (tên, mã, tầng, loại, mô tả)</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRoomRequest request, CancellationToken ct)
    {
        var result = await update.HandleAsync(id, request, ct);
        return Ok(result);
    }

    /// <summary>DELETE api/rooms/{id} — Xóa phòng</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await delete.HandleAsync(id, ct);
        return NoContent();
    }

    /// <summary>PATCH api/rooms/{id}/status — Đổi trạng thái phòng</summary>
    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> ChangeStatus(Guid id, [FromBody] ChangeRoomStatusRequest request, CancellationToken ct)
    {
        var result = await changeStatus.HandleAsync(id, request, ct);
        return Ok(result);
    }
}
