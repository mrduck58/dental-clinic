using DentalClinic.API.Application.DTOs.Inventory;
using DentalClinic.API.Domain.Interfaces.Repositories;

namespace DentalClinic.API.Application.UseCases.Inventory;

public class GetSupplyItemsHandler(ISupplyItemRepository repository)
{
    public async Task<IEnumerable<SupplyItemDto>> HandleAsync(
        string? search,
        string? category,
        CancellationToken ct = default)
    {
        var items = await repository.GetAllAsync(ct);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.ToLower();
            items = items.Where(i => i.Name.ToLower().Contains(q) || i.Code.ToLower().Contains(q));
        }

        if (!string.IsNullOrWhiteSpace(category) && category != "Tất cả")
            items = items.Where(i => i.Category == category);

        return items.Select(ToDto);
    }

    internal static SupplyItemDto ToDto(Domain.Entities.SupplyItem i) =>
        new(i.Id, i.Code, i.Name, i.Category, i.Unit, i.Quantity, i.MinQuantity, i.Quantity <= i.MinQuantity, i.OrderType, i.Price, i.CreatedAt, i.UpdatedAt);
}
