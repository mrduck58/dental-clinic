using DentalClinic.API.Application.DTOs.Promotions;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;

namespace DentalClinic.API.Application.UseCases.Promotions;

public class CreatePromotionHandler(IPromotionRepository repo)
{
    public async Task<Guid> HandleAsync(CreatePromotionRequest req, CancellationToken ct = default)
    {
        var promotion = Promotion.Create(
            req.Code, req.Name, req.Description,
            req.DiscountType, req.DiscountValue,
            req.ServiceIds, req.StartDate, req.EndDate,
            req.IsActive);
        await repo.AddAsync(promotion, ct);
        return promotion.Id;
    }
}
