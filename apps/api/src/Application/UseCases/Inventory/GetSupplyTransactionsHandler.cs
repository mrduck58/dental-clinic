using DentalClinic.API.Application.DTOs.Inventory;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Inventory;

/// <summary>RoomId khác null → chỉ trả về giao dịch xuất theo đúng phòng đó (dùng cho màn chi tiết phòng
/// bên Admin, xem `admin/rooms/[id]`).</summary>
public record GetSupplyTransactionsQuery(Guid? RoomId = null) : IRequest<IEnumerable<SupplyTransactionDto>>;

public class GetSupplyTransactionsHandler(ISupplyTransactionRepository repository) : IRequestHandler<GetSupplyTransactionsQuery, IEnumerable<SupplyTransactionDto>>
{
    public async Task<IEnumerable<SupplyTransactionDto>> Handle(GetSupplyTransactionsQuery request, CancellationToken ct)
    {
        var txs = await repository.GetAllAsync(request.RoomId, ct);

        return txs.Select(t => new SupplyTransactionDto(
            t.Id,
            t.SupplyItemId,
            t.SupplyItem.Name,
            t.Type,
            t.Quantity,
            t.UnitPrice,
            t.Note,
            t.CreatedBy,
            t.CreatedAt,
            t.Room?.Name));
    }
}
