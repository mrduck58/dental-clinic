using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Domain.Constants;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Promotions;

public record UpdatePromotionCommand(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    string DiscountType,
    decimal DiscountValue,
    List<Guid> ServiceIds,
    DateOnly StartDate,
    DateOnly EndDate) : IRequest<bool>;

public class UpdatePromotionHandler(
    IPromotionRepository repo,
    IActivityLogService activityLogService,
    ICurrentUserService currentUser) : IRequestHandler<UpdatePromotionCommand, bool>
{
    public async Task<bool> Handle(UpdatePromotionCommand request, CancellationToken cancellationToken)
    {
        var promotion = await repo.GetByIdAsync(request.Id, cancellationToken);
        if (promotion is null) return false;

        promotion.Update(request.Code, request.Name, request.Description,
            request.DiscountType, request.DiscountValue,
            request.ServiceIds, request.StartDate, request.EndDate);
        await repo.UpdateAsync(promotion, cancellationToken);

        await activityLogService.LogAsync(
            userId: currentUser.UserId,
            userName: currentUser.UserName,
            userRole: currentUser.UserRole,
            action: ActivityAction.Edit,
            module: ActivityModule.Promotion,
            description: $"Cập nhật khuyến mãi: {promotion.Name}",
            status: ActivityStatus.Success,
            ipAddress: currentUser.IpAddress,
            targetId: request.Id.ToString(),
            ct: cancellationToken);

        return true;
    }
}
