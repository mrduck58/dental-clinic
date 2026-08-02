using DentalClinic.API.Application.DTOs.Promotions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Promotions;

public record GetPromotionsQuery : IRequest<IEnumerable<PromotionDto>>;

public class GetPromotionsHandler(IPromotionRepository repo, IServiceRepository serviceRepo)
    : IRequestHandler<GetPromotionsQuery, IEnumerable<PromotionDto>>
{
    public async Task<IEnumerable<PromotionDto>> Handle(GetPromotionsQuery request, CancellationToken cancellationToken)
    {
        var promotions = await repo.GetAllAsync(cancellationToken);
        var services = (await serviceRepo.GetAllAsync(cancellationToken)).ToDictionary(s => s.Id, s => s.Name);
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
