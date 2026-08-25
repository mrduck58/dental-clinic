using DentalClinic.API.Application.DTOs.Rooms;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Rooms;

public record GetRoomsQuery(string? Floor, string? Status, string? Search) : IRequest<IEnumerable<RoomDto>>;

public class GetRoomsHandler(IRoomRepository roomRepository) : IRequestHandler<GetRoomsQuery, IEnumerable<RoomDto>>
{
    public async Task<IEnumerable<RoomDto>> Handle(GetRoomsQuery query, CancellationToken ct)
    {
        var rooms = await roomRepository.GetAllAsync(ct);

        if (!string.IsNullOrWhiteSpace(query.Floor))
            rooms = rooms.Where(r => r.Floor == query.Floor);

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            try
            {
                var roomStatus = RoomStatusMapper.FromVietnamese(query.Status);
                rooms = rooms.Where(r => r.Status == roomStatus);
            }
            catch (ArgumentException) { }
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var q = query.Search.ToLower();
            rooms = rooms.Where(r =>
                r.Name.ToLower().Contains(q) ||
                r.Code.ToLower().Contains(q));
        }

        return rooms.Select(r => new RoomDto(
            r.Id, r.Code, r.Name, r.Floor,
            r.Status.ToVietnamese(), r.Status.ToActiveStatus(),
            r.Description, r.CreatedAt, r.UpdatedAt));
    }
}
