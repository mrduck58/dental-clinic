using DentalClinic.API.Application.DTOs.Rooms;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;

namespace DentalClinic.API.Application.UseCases.Rooms;

public class ChangeRoomStatusHandler(IRoomRepository roomRepository)
{
    public async Task<RoomDto> HandleAsync(Guid id, ChangeRoomStatusRequest request, CancellationToken ct = default)
    {
        var room = await roomRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Không tìm thấy phòng với ID: {id}");

        var newStatus = RoomStatusMapper.FromVietnamese(request.Status);
        room.ChangeStatus(newStatus);
        await roomRepository.UpdateAsync(room, ct);

        return new RoomDto(
            room.Id, room.Code, room.Name, room.Floor, room.Type,
            room.Status.ToVietnamese(), room.Status.ToActiveStatus(),
            room.Description, room.CreatedAt, room.UpdatedAt);
    }
}
