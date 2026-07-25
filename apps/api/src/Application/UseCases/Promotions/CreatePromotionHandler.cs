using DentalClinic.API.Application.DTOs.Promotions;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Domain.Constants;

namespace DentalClinic.API.Application.UseCases.Promotions;

public class CreatePromotionHandler(
    IPromotionRepository repo,
    IActivityLogService activityLogService,
    ICurrentUserService currentUser)
{
    public async Task<Guid> HandleAsync(CreatePromotionRequest req, CancellationToken ct = default)
    {
        var promotion = Promotion.Create(
            req.Code, req.Name, req.Description,
            req.DiscountType, req.DiscountValue,
            req.ServiceIds, req.StartDate, req.EndDate,
            req.IsActive);
        await repo.AddAsync(promotion, ct);

        await activityLogService.LogAsync(
            userId: currentUser.UserId,
            userName: currentUser.UserName,
            userRole: currentUser.UserRole,
            action: ActivityAction.Create,
            module: ActivityModule.Promotion,
            description: $"Tạo khuyến mãi: {req.Name} (mã: {req.Code})",
            status: ActivityStatus.Success,
            ipAddress: currentUser.IpAddress,
            targetId: promotion.Id.ToString(),
            ct: ct);

        return promotion.Id;
    }
}
