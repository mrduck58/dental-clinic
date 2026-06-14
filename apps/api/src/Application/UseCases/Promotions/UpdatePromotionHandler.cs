using DentalClinic.API.Application.DTOs.Promotions;
using DentalClinic.API.Domain.Interfaces.Repositories;

namespace DentalClinic.API.Application.UseCases.Promotions;

public class UpdatePromotionHandler(IPromotionRepository repo)
{
    public async Task<bool> HandleAsync(Guid id, UpdatePromotionRequest req, CancellationToken ct = default)
    {
        var promotion = await repo.GetByIdAsync(id, ct);
        if (promotion is null) return false;
        promotion.Update(req.Code, req.Name, req.Description,
            req.DiscountType, req.DiscountValue,
            req.ServiceIds, req.StartDate, req.EndDate);
        await repo.UpdateAsync(promotion, ct);
        return true;
    }
}
