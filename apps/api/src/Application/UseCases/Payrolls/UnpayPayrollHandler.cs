using DentalClinic.API.Application.DTOs.Payrolls;
using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Payrolls;

/// <summary>Hoàn tác một lần chi trả (đưa bảng lương của kỳ về trạng thái chờ duyệt).</summary>
public record UnpayPayrollCommand(int Year, int Month, Guid UserId) : IRequest<PayrollItemDto>;

public class UnpayPayrollHandler(
    IPayrollRepository payrollRepository,
    IActivityLogService activityLogService,
    ICurrentUserService currentUser) : IRequestHandler<UnpayPayrollCommand, PayrollItemDto>
{
    public async Task<PayrollItemDto> Handle(UnpayPayrollCommand command, CancellationToken ct)
    {
        var record = await payrollRepository.GetByUserAndPeriodAsync(command.UserId, command.Year, command.Month, ct)
            ?? throw new NotFoundException(
                $"Không tìm thấy bảng lương tháng {command.Month}/{command.Year} của nhân sự này.");

        record.MarkUnpaid();
        await payrollRepository.SaveChangesAsync(ct);

        var (from, to) = GetPayrollPeriodHandler.PeriodRange(command.Year, command.Month);
        var users = await payrollRepository.GetPayableUsersAsync(ct);
        var user = users.FirstOrDefault(u => u.Id == command.UserId)
            ?? throw new NotFoundException($"Không tìm thấy nhân sự với ID: {command.UserId}");
        var leaves = await payrollRepository.GetApprovedLeavesOverlappingAsync(from, to, ct);

        await activityLogService.LogAsync(
            userId: currentUser.UserId,
            userName: currentUser.UserName,
            userRole: currentUser.UserRole,
            action: ActivityAction.Cancel,
            module: ActivityModule.Payroll,
            description: $"Hoàn tác chi trả lương tháng {command.Month}/{command.Year} của {user.FullName}",
            status: ActivityStatus.Success,
            ipAddress: currentUser.IpAddress,
            targetId: record.Id.ToString(),
            ct: ct);

        // record luôn khác null tại đây nên requiredShifts không được dùng tới trong BuildItem.
        return GetPayrollPeriodHandler.BuildItem(user, record, leaves, 0, command.Year, command.Month);
    }
}
