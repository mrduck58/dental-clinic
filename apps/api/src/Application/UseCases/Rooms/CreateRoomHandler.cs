using DentalClinic.API.Application.DTOs.Rooms;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;

namespace DentalClinic.API.Application.UseCases.Rooms;

public class CreateRoomHandler(IRoomRepository roomRepository)
{
    public async Task<RoomDto> HandleAsync(CreateRoomRequest request, CancellationToken ct = default)
    {
        if (await roomRepository.ExistsByCodeAsync(request.Code, ct: ct))
            throw new ConflictException($"Mã phòng '{request.Code.ToUpperInvariant()}' đã tồn tại.");

        if (await roomRepository.ExistsByNameAsync(request.Name, ct: ct))
            throw new ConflictException($"Tên phòng '{request.Name}' đã tồn tại.");

        var room = Room.Create(
            request.Code,
            request.Name,
            request.Floor,
            request.Type,
            request.Description);

        await roomRepository.AddAsync(room, ct);

        return new RoomDto(
            room.Id, room.Code, room.Name, room.Floor, room.Type,
            room.Status.ToVietnamese(), room.Status.ToActiveStatus(),
            room.Description, room.CreatedAt, room.UpdatedAt);
    }
}
