using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Commissions;

public record ToggleCommissionRuleActiveCommand(Guid Id) : IRequest;

public class ToggleCommissionRuleActiveHandler(
    ICommissionRuleRepository commissionRuleRepository,
    IActivityLogService activityLogService,
    ICurrentUserService currentUser) : IRequestHandler<ToggleCommissionRuleActiveCommand>
{
    public async Task Handle(ToggleCommissionRuleActiveCommand command, CancellationToken ct)
    {
        var rule = await commissionRuleRepository.GetByIdAsync(command.Id, ct)
            ?? throw new NotFoundException("Không tìm thấy quy tắc hoa hồng.");

        if (rule.IsActive) rule.Deactivate(); else rule.Activate();
        await commissionRuleRepository.SaveChangesAsync(ct);

        await activityLogService.LogAsync(
            userId: currentUser.UserId,
            userName: currentUser.UserName,
            userRole: currentUser.UserRole,
            action: ActivityAction.Edit,
            module: ActivityModule.Commission,
            description: $"{(rule.IsActive ? "Bật" : "Tắt")} quy tắc hoa hồng {rule.RatePercent}%",
            status: ActivityStatus.Success,
            ipAddress: currentUser.IpAddress,
            targetId: rule.Id.ToString(),
            ct: ct);
    }
}
