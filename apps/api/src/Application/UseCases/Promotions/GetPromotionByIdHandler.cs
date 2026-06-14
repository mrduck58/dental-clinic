using DentalClinic.API.Application.DTOs.Promotions;
using DentalClinic.API.Domain.Interfaces.Repositories;

namespace DentalClinic.API.Application.UseCases.Promotions;

public class GetPromotionByIdHandler(IPromotionRepository repo, IServiceRepository serviceRepo)
{
    public async Task<PromotionDto?> HandleAsync(Guid id, CancellationToken ct = default)
    {
        var p = await repo.GetByIdAsync(id, ct);
        if (p is null) return null;
        var services = (await serviceRepo.GetAllAsync(ct)).ToDictionary(s => s.Id, s => s.Name);
        var ids = p.GetServiceIds();
        var names = ids.Count == 0
            ? (List<string>)["Tat ca dich vu"]
            : ids.Select(sid => services.TryGetValue(sid, out var n) ? n : sid.ToString()).ToList();
        return new PromotionDto(p.Id, p.Code, p.Name, p.Description, p.DiscountType,
            p.DiscountValue, ids, names, p.StartDate, p.EndDate, p.IsActive, p.CreatedAt, p.UpdatedAt);
    }
}
