using DentalClinic.API.Application.DTOs.Payrolls;
using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Payrolls;

/// <summary>Tạo kỳ lương (Draft) cho các nhân sự CHƯA có bản ghi trong kỳ — an toàn để gọi lại nhiều lần
/// (ví dụ khi có nhân sự mới), bỏ qua những người đã có bản ghi ở bất kỳ trạng thái nào.</summary>
public record CreatePayrollPeriodCommand(int Year, int Month) : IRequest<PayrollPeriodActionResult>;

public class CreatePayrollPeriodHandler(
    IPayrollRepository payrollRepository,
    IActivityLogService activityLogService,
    ICurrentUserService currentUser) : IRequestHandler<CreatePayrollPeriodCommand, PayrollPeriodActionResult>
{
    public async Task<PayrollPeriodActionResult> Handle(CreatePayrollPeriodCommand command, CancellationToken ct)
    {
        if (command.Month is < 1 or > 12)
            throw new ValidationException("Tháng phải nằm trong khoảng 1–12.");

        var (from, to) = GetPayrollPeriodHandler.PeriodRange(command.Year, command.Month);
        var users = await payrollRepository.GetPayableUsersAsync(ct);
        var existingRecords = await payrollRepository.GetByPeriodAsync(command.Year, command.Month, ct);
        var existingUserIds = existingRecords.Select(r => r.UserId).ToHashSet();
        var leaves = await payrollRepository.GetApprovedLeavesOverlappingAsync(from, to, ct);

        var newRecords = new List<PayrollRecord>();
        foreach (var user in users)
        {
            if (existingUserIds.Contains(user.Id)) continue;

            var c = PayrollCalculator.Compute(user, leaves, command.Year, command.Month);
            newRecords.Add(PayrollRecord.CreateDraft(
                user.Id, command.Year, command.Month,
                c.BaseSalary, c.Allowance, c.LeaveShifts, c.AllowedLeaveShifts, c.ExceededShifts, c.Deduction));
        }

        if (newRecords.Count > 0)
            await payrollRepository.AddRangeAsync(newRecords, ct);

        await activityLogService.LogAsync(
            userId: currentUser.UserId,
            userName: currentUser.UserName,
            userRole: currentUser.UserRole,
            action: ActivityAction.Create,
            module: ActivityModule.Payroll,
            description: $"Tạo kỳ lương tháng {command.Month}/{command.Year} cho {newRecords.Count} nhân sự",
            status: ActivityStatus.Success,
            ipAddress: currentUser.IpAddress,
            targetId: $"{command.Year}-{command.Month:D2}",
            ct: ct);

        return new PayrollPeriodActionResult(newRecords.Count, existingUserIds.Count, []);
    }
}
