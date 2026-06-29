using DentalClinic.API.Application.DTOs.Promotions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Domain.Constants;

namespace DentalClinic.API.Application.UseCases.Promotions;

public class UpdatePromotionHandler(
    IPromotionRepository repo,
    IActivityLogService activityLogService,
    ICurrentUserService currentUser)
{
    public async Task<bool> HandleAsync(Guid id, UpdatePromotionRequest req, CancellationToken ct = default)
    {
        var promotion = await repo.GetByIdAsync(id, ct);
        if (promotion is null) return false;

        promotion.Update(req.Code, req.Name, req.Description,
            req.DiscountType, req.DiscountValue,
            req.ServiceIds, req.StartDate, req.EndDate);
        await repo.UpdateAsync(promotion, ct);

        await activityLogService.LogAsync(
            userId: currentUser.UserId,
            userName: currentUser.UserName,
            userRole: currentUser.UserRole,
            action: ActivityAction.Edit,
            module: ActivityModule.Promotion,
            description: $"Cập nhật khuyến mãi: {promotion.Name}",
            status: ActivityStatus.Success,
            ipAddress: currentUser.IpAddress,
            targetId: id.ToString(),
            ct: ct);

        return true;
    }
}
