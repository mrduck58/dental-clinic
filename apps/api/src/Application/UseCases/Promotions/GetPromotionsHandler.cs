using DentalClinic.API.Application.DTOs.Promotions;
using DentalClinic.API.Domain.Interfaces.Repositories;

namespace DentalClinic.API.Application.UseCases.Promotions;

public class GetPromotionsHandler(IPromotionRepository repo, IServiceRepository serviceRepo)
{
    public async Task<IEnumerable<PromotionDto>> HandleAsync(CancellationToken ct = default)
    {
        var promotions = await repo.GetAllAsync(ct);
        var services = (await serviceRepo.GetAllAsync(ct)).ToDictionary(s => s.Id, s => s.Name);
        return promotions.Select(p =>
        {
            var ids = p.GetServiceIds();
            var names = ids.Count == 0
                ? (List<string>)["Tat ca dich vu"]
                : ids.Select(id => services.TryGetValue(id, out var n) ? n : id.ToString()).ToList();
            return new PromotionDto(p.Id, p.Code, p.Name, p.Description, p.DiscountType,
                p.DiscountValue, ids, names, p.StartDate, p.EndDate, p.IsActive, p.CreatedAt, p.UpdatedAt);
        });
    }
}
