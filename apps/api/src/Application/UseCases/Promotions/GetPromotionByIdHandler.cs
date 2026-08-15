using DentalClinic.API.Application.DTOs.Promotions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Promotions;

public record GetPromotionByIdQuery(Guid Id) : IRequest<PromotionDto?>;

public class GetPromotionByIdHandler(IPromotionRepository repo, IServiceRepository serviceRepo)
    : IRequestHandler<GetPromotionByIdQuery, PromotionDto?>
{
    public async Task<PromotionDto?> Handle(GetPromotionByIdQuery request, CancellationToken cancellationToken)
    {
        var p = await repo.GetByIdAsync(request.Id, cancellationToken);
        if (p is null) return null;
        var services = await serviceRepo.GetIdNameMapAsync(cancellationToken);
        var ids = p.GetServiceIds();
        var names = ids.Count == 0
            ? (List<string>)["Tat ca dich vu"]
            : ids.Select(sid => services.TryGetValue(sid, out var n) ? n : sid.ToString()).ToList();
        return new PromotionDto(p.Id, p.Code, p.Name, p.Description, p.DiscountType,
            p.DiscountValue, ids, names, p.StartDate, p.EndDate, p.IsActive, p.CreatedAt, p.UpdatedAt);
    }
}
