using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;

namespace DentalClinic.API.Application.UseCases.Promotions;

public class DeletePromotionHandler(
    IPromotionRepository repo,
    IActivityLogService activityLogService,
    ICurrentUserService currentUser)
{
    public async Task<bool> HandleAsync(Guid id, CancellationToken ct = default)
    {
        var promotion = await repo.GetByIdAsync(id, ct);
        if (promotion is null) return false;

        await repo.DeleteAsync(promotion, ct);

        await activityLogService.LogAsync(
            userId: currentUser.UserId,
            userName: currentUser.UserName,
            userRole: currentUser.UserRole,
            action: ActivityAction.Delete,
            module: ActivityModule.Promotion,
            description: $"Xóa khuyến mãi: {promotion.Name}",
            status: ActivityStatus.Success,
            ipAddress: currentUser.IpAddress,
            targetId: id.ToString(),
            ct: ct);

        return true;
    }
}
