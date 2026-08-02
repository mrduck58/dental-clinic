using DentalClinic.API.Application.DTOs.Rooms;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Rooms;

public record GetRoomByIdQuery(Guid Id) : IRequest<RoomDto>;

public class GetRoomByIdHandler(IRoomRepository roomRepository) : IRequestHandler<GetRoomByIdQuery, RoomDto>
{
    public async Task<RoomDto> Handle(GetRoomByIdQuery query, CancellationToken ct)
    {
        var room = await roomRepository.GetByIdAsync(query.Id, ct)
            ?? throw new NotFoundException($"Không tìm thấy phòng với ID: {query.Id}");

        return new RoomDto(
            room.Id, room.Code, room.Name, room.Floor, room.Type,
            room.Status.ToVietnamese(), room.Status.ToActiveStatus(),
            room.Description, room.CreatedAt, room.UpdatedAt);
    }
}
