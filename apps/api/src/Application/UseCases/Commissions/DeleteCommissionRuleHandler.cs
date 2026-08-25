using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Commissions;

public record DeleteCommissionRuleCommand(Guid Id) : IRequest;

public class DeleteCommissionRuleHandler(
    ICommissionRuleRepository commissionRuleRepository,
    IActivityLogService activityLogService,
    ICurrentUserService currentUser) : IRequestHandler<DeleteCommissionRuleCommand>
{
    public async Task Handle(DeleteCommissionRuleCommand command, CancellationToken ct)
    {
        var rule = await commissionRuleRepository.GetByIdAsync(command.Id, ct)
            ?? throw new NotFoundException("Không tìm thấy quy tắc hoa hồng.");

        await commissionRuleRepository.DeleteAsync(rule, ct);
        await commissionRuleRepository.SaveChangesAsync(ct);

        await activityLogService.LogAsync(
            userId: currentUser.UserId,
            userName: currentUser.UserName,
            userRole: currentUser.UserRole,
            action: ActivityAction.Delete,
            module: ActivityModule.Commission,
            description: $"Xoá quy tắc hoa hồng {rule.RatePercent}%",
            status: ActivityStatus.Success,
            ipAddress: currentUser.IpAddress,
            targetId: rule.Id.ToString(),
            ct: ct);
    }
}
