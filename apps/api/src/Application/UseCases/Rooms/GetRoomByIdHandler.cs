using DentalClinic.API.Application.DTOs.Rooms;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;

namespace DentalClinic.API.Application.UseCases.Rooms;

public class GetRoomByIdHandler(IRoomRepository roomRepository)
{
    public async Task<RoomDto> HandleAsync(Guid id, CancellationToken ct = default)
    {
        var room = await roomRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Không tìm thấy phòng với ID: {id}");

        return new RoomDto(
            room.Id, room.Code, room.Name, room.Floor, room.Type,
            room.Status.ToVietnamese(), room.Status.ToActiveStatus(),
            room.Description, room.CreatedAt, room.UpdatedAt);
    }
}
