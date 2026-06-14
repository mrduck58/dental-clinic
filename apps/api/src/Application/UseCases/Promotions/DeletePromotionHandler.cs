using DentalClinic.API.Domain.Interfaces.Repositories;

namespace DentalClinic.API.Application.UseCases.Promotions;

public class DeletePromotionHandler(IPromotionRepository repo)
{
    public async Task<bool> HandleAsync(Guid id, CancellationToken ct = default)
    {
        var promotion = await repo.GetByIdAsync(id, ct);
        if (promotion is null) return false;
        await repo.DeleteAsync(promotion, ct);
        return true;
    }
}
