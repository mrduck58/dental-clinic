using DentalClinic.API.Application.DTOs.Inventory;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Inventory;

public record GetSupplyItemsQuery(string? Search = null, string? Category = null) : IRequest<IEnumerable<SupplyItemDto>>;

public class GetSupplyItemsHandler(ISupplyItemRepository repository) : IRequestHandler<GetSupplyItemsQuery, IEnumerable<SupplyItemDto>>
{
    public async Task<IEnumerable<SupplyItemDto>> Handle(GetSupplyItemsQuery query, CancellationToken ct)
    {
        var items = await repository.GetAllAsync(ct);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var q = query.Search.ToLower();
            items = items.Where(i => i.Name.ToLower().Contains(q) || i.Code.ToLower().Contains(q));
        }

        if (!string.IsNullOrWhiteSpace(query.Category) && query.Category != "Tất cả")
            items = items.Where(i => i.Category == query.Category);

        return items.Select(ToDto);
    }

    internal static SupplyItemDto ToDto(Domain.Entities.SupplyItem i) =>
        new(i.Id, i.Code, i.Name, i.Category, i.Unit, i.Quantity, i.MinQuantity, i.Quantity <= i.MinQuantity, i.OrderType, i.Price, i.CreatedAt, i.UpdatedAt);
}
