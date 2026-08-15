using DentalClinic.API.Application.DTOs.Commissions;
using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Commissions;

public record CreateCommissionRuleCommand(CommissionRuleRequest Request) : IRequest<Guid>;

public class CreateCommissionRuleHandler(
    ICommissionRuleRepository commissionRuleRepository,
    IActivityLogService activityLogService,
    ICurrentUserService currentUser) : IRequestHandler<CreateCommissionRuleCommand, Guid>
{
    public async Task<Guid> Handle(CreateCommissionRuleCommand command, CancellationToken ct)
    {
        var r = command.Request;
        var rule = CommissionRule.Create(r.DentistId, r.ServiceName, r.RatePercent, r.EffectiveFrom, r.EffectiveTo, r.Note);
        await commissionRuleRepository.AddAsync(rule, ct);
        await commissionRuleRepository.SaveChangesAsync(ct);

        await activityLogService.LogAsync(
            userId: currentUser.UserId,
            userName: currentUser.UserName,
            userRole: currentUser.UserRole,
            action: ActivityAction.Create,
            module: ActivityModule.Commission,
            description: $"Tạo quy tắc hoa hồng {rule.RatePercent}%",
            status: ActivityStatus.Success,
            ipAddress: currentUser.IpAddress,
            targetId: rule.Id.ToString(),
            ct: ct);

        return rule.Id;
    }
}
