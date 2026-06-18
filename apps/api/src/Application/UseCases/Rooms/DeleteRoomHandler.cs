using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;

namespace DentalClinic.API.Application.UseCases.Rooms;

public class DeleteRoomHandler(IRoomRepository roomRepository)
{
    public async Task HandleAsync(Guid id, CancellationToken ct = default)
    {
        var room = await roomRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Không tìm thấy phòng với ID: {id}");

        await roomRepository.DeleteAsync(room, ct);
    }
}
