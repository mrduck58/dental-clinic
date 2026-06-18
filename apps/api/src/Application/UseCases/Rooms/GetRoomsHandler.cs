using DentalClinic.API.Application.DTOs.Rooms;
using DentalClinic.API.Domain.Interfaces.Repositories;

namespace DentalClinic.API.Application.UseCases.Rooms;

public class GetRoomsHandler(IRoomRepository roomRepository)
{
    public async Task<IEnumerable<RoomDto>> HandleAsync(
        string? floor,
        string? status,
        string? search,
        CancellationToken ct = default)
    {
        var rooms = await roomRepository.GetAllAsync(ct);

        if (!string.IsNullOrWhiteSpace(floor))
            rooms = rooms.Where(r => r.Floor == floor);

        if (!string.IsNullOrWhiteSpace(status))
        {
            try
            {
                var roomStatus = RoomStatusMapper.FromVietnamese(status);
                rooms = rooms.Where(r => r.Status == roomStatus);
            }
            catch (ArgumentException) { }
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.ToLower();
            rooms = rooms.Where(r =>
                r.Name.ToLower().Contains(q) ||
                r.Code.ToLower().Contains(q) ||
                r.Type.ToLower().Contains(q));
        }

        return rooms.Select(r => new RoomDto(
            r.Id, r.Code, r.Name, r.Floor, r.Type,
            r.Status.ToVietnamese(), r.Status.ToActiveStatus(),
            r.Description, r.CreatedAt, r.UpdatedAt));
    }
}
