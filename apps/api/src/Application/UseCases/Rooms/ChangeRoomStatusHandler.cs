using DentalClinic.API.Application.DTOs.Rooms;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Rooms;

public record ChangeRoomStatusCommand(Guid Id, ChangeRoomStatusRequest Request) : IRequest<RoomDto>;

public class ChangeRoomStatusHandler(IRoomRepository roomRepository) : IRequestHandler<ChangeRoomStatusCommand, RoomDto>
{
    public async Task<RoomDto> Handle(ChangeRoomStatusCommand command, CancellationToken ct)
    {
        var room = await roomRepository.GetByIdAsync(command.Id, ct)
            ?? throw new NotFoundException($"Không tìm thấy phòng với ID: {command.Id}");

        var newStatus = RoomStatusMapper.FromVietnamese(command.Request.Status);
        room.ChangeStatus(newStatus);
        await roomRepository.UpdateAsync(room, ct);

        return new RoomDto(
            room.Id, room.Code, room.Name, room.Floor,
            room.Status.ToVietnamese(), room.Status.ToActiveStatus(),
            room.Description, room.CreatedAt, room.UpdatedAt);
    }
}
