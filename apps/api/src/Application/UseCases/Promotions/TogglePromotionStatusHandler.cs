using DentalClinic.API.Application.DTOs.Promotions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Promotions;

public record TogglePromotionStatusCommand(Guid Id) : IRequest<PromotionDto?>;

public class TogglePromotionStatusHandler(IPromotionRepository repo, IServiceRepository serviceRepo)
    : IRequestHandler<TogglePromotionStatusCommand, PromotionDto?>
{
    public async Task<PromotionDto?> Handle(TogglePromotionStatusCommand request, CancellationToken cancellationToken)
    {
        var p = await repo.GetByIdAsync(request.Id, cancellationToken);
        if (p is null) return null;
        p.SetActive(!p.IsActive);
        await repo.UpdateAsync(p, cancellationToken);
        var services = (await serviceRepo.GetAllAsync(cancellationToken)).ToDictionary(s => s.Id, s => s.Name);
        var ids = p.GetServiceIds();
        var names = ids.Count == 0
            ? (List<string>)["Tat ca dich vu"]
            : ids.Select(sid => services.TryGetValue(sid, out var n) ? n : sid.ToString()).ToList();
        return new PromotionDto(p.Id, p.Code, p.Name, p.Description, p.DiscountType,
            p.DiscountValue, ids, names, p.StartDate, p.EndDate, p.IsActive, p.CreatedAt, p.UpdatedAt);
    }
}
