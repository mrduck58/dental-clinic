using DentalClinic.API.Application.DTOs.Payrolls;
using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Payrolls;

/// <summary>Sửa Thưởng của một nhân sự trong kỳ — chỉ áp dụng được khi kỳ còn ở trạng thái Nháp.</summary>
public record SetPayrollBonusCommand(int Year, int Month, Guid UserId, decimal Bonus) : IRequest<PayrollItemDto>;

public class SetPayrollBonusHandler(
    IPayrollRepository payrollRepository,
    IActivityLogService activityLogService,
    ICurrentUserService currentUser) : IRequestHandler<SetPayrollBonusCommand, PayrollItemDto>
{
    public async Task<PayrollItemDto> Handle(SetPayrollBonusCommand command, CancellationToken ct)
    {
        var record = await payrollRepository.GetByUserAndPeriodAsync(command.UserId, command.Year, command.Month, ct)
            ?? throw new NotFoundException($"Kỳ lương tháng {command.Month}/{command.Year} của nhân sự này chưa được tạo.");

        record.SetBonus(command.Bonus);
        await payrollRepository.SaveChangesAsync(ct);

        var users = await payrollRepository.GetPayableUsersAsync(ct);
        var user = users.FirstOrDefault(u => u.Id == command.UserId)
            ?? throw new NotFoundException($"Không tìm thấy nhân sự với ID: {command.UserId}");

        await activityLogService.LogAsync(
            userId: currentUser.UserId,
            userName: currentUser.UserName,
            userRole: currentUser.UserRole,
            action: ActivityAction.Edit,
            module: ActivityModule.Payroll,
            description: $"Cập nhật Thưởng tháng {command.Month}/{command.Year} cho {user.FullName}: {command.Bonus:N0} đ",
            status: ActivityStatus.Success,
            ipAddress: currentUser.IpAddress,
            targetId: record.Id.ToString(),
            ct: ct);

        return GetPayrollPeriodHandler.BuildItem(user, record, [], command.Year, command.Month);
    }
}
