using DentalClinic.API.Application.DTOs.Commissions;
using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Commissions;

public record UpdateCommissionRuleCommand(Guid Id, CommissionRuleRequest Request) : IRequest;

public class UpdateCommissionRuleHandler(
    ICommissionRuleRepository commissionRuleRepository,
    IActivityLogService activityLogService,
    ICurrentUserService currentUser) : IRequestHandler<UpdateCommissionRuleCommand>
{
    public async Task Handle(UpdateCommissionRuleCommand command, CancellationToken ct)
    {
        var rule = await commissionRuleRepository.GetByIdAsync(command.Id, ct)
            ?? throw new NotFoundException("Không tìm thấy quy tắc hoa hồng.");

        var r = command.Request;
        rule.Update(r.DentistId, r.ServiceName, r.RatePercent, r.EffectiveFrom, r.EffectiveTo, r.Note);
        await commissionRuleRepository.SaveChangesAsync(ct);

        await activityLogService.LogAsync(
            userId: currentUser.UserId,
            userName: currentUser.UserName,
            userRole: currentUser.UserRole,
            action: ActivityAction.Edit,
            module: ActivityModule.Commission,
            description: $"Sửa quy tắc hoa hồng {rule.RatePercent}%",
            status: ActivityStatus.Success,
            ipAddress: currentUser.IpAddress,
            targetId: rule.Id.ToString(),
            ct: ct);
    }
}
