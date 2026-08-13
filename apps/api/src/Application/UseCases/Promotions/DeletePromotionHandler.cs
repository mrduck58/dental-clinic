using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Domain.Constants;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Promotions;

public record DeletePromotionCommand(Guid Id) : IRequest<bool>;

public class DeletePromotionHandler(
    IPromotionRepository repo,
    IActivityLogService activityLogService,
    ICurrentUserService currentUser) : IRequestHandler<DeletePromotionCommand, bool>
{
    public async Task<bool> Handle(DeletePromotionCommand request, CancellationToken cancellationToken)
    {
        var promotion = await repo.GetByIdAsync(request.Id, cancellationToken);
        if (promotion is null) return false;

        await repo.DeleteAsync(promotion, cancellationToken);

        await activityLogService.LogAsync(
            userId: currentUser.UserId,
            userName: currentUser.UserName,
            userRole: currentUser.UserRole,
            action: ActivityAction.Delete,
            module: ActivityModule.Promotion,
            description: $"Xóa khuyến mãi: {promotion.Name}",
            status: ActivityStatus.Success,
            ipAddress: currentUser.IpAddress,
            targetId: request.Id.ToString(),
            ct: cancellationToken);

        return true;
    }
}
