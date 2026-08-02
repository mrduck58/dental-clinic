using DentalClinic.API.Application.DTOs.Inventory;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Inventory;

public record GetSupplyTransactionsQuery : IRequest<IEnumerable<SupplyTransactionDto>>;

public class GetSupplyTransactionsHandler(ISupplyTransactionRepository repository) : IRequestHandler<GetSupplyTransactionsQuery, IEnumerable<SupplyTransactionDto>>
{
    public async Task<IEnumerable<SupplyTransactionDto>> Handle(GetSupplyTransactionsQuery request, CancellationToken ct)
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
