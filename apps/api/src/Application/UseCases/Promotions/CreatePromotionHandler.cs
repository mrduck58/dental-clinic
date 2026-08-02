using DentalClinic.API.Application.DTOs.Promotions;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Domain.Constants;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Promotions;

public record CreatePromotionCommand(
    string Code,
    string Name,
    string? Description,
    string DiscountType,
    decimal DiscountValue,
    List<Guid> ServiceIds,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsActive) : IRequest<Guid>;

public class CreatePromotionHandler(
    IPromotionRepository repo,
    IActivityLogService activityLogService,
    ICurrentUserService currentUser) : IRequestHandler<CreatePromotionCommand, Guid>
{
    public async Task<Guid> Handle(CreatePromotionCommand request, CancellationToken cancellationToken)
    {
        var promotion = Promotion.Create(
            request.Code, request.Name, request.Description,
            request.DiscountType, request.DiscountValue,
            request.ServiceIds, request.StartDate, request.EndDate,
            request.IsActive);
        await repo.AddAsync(promotion, cancellationToken);

        await activityLogService.LogAsync(
            userId: currentUser.UserId,
            userName: currentUser.UserName,
            userRole: currentUser.UserRole,
            action: ActivityAction.Create,
            module: ActivityModule.Promotion,
            description: $"Tạo khuyến mãi: {request.Name} (mã: {request.Code})",
            status: ActivityStatus.Success,
            ipAddress: currentUser.IpAddress,
            targetId: promotion.Id.ToString(),
            ct: cancellationToken);

        return promotion.Id;
    }
}
