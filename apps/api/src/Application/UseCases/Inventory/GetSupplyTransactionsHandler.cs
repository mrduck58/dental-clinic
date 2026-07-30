using DentalClinic.API.Application.DTOs.Inventory;
using DentalClinic.API.Domain.Interfaces.Repositories;

namespace DentalClinic.API.Application.UseCases.Inventory;

public class GetSupplyTransactionsHandler(ISupplyTransactionRepository repository)
{
    public async Task<IEnumerable<SupplyTransactionDto>> HandleAsync(CancellationToken ct = default)
    {
        var txs = await repository.GetAllAsync(ct);

        return txs.Select(t => new SupplyTransactionDto(
            t.Id,
            t.SupplyItemId,
            t.SupplyItem.Name,
            t.Type,
            t.Quantity,
            t.UnitPrice,
            t.Note,
            t.CreatedBy,
            t.CreatedAt));
    }
}
