using DentalClinic.API.Application.DTOs.Payrolls;
using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Payrolls;

/// <summary>Owner duyệt kỳ lương — chuyển các bản ghi Đã tính của kỳ sang Đã duyệt, đủ điều kiện chi trả.</summary>
public record ApprovePayrollPeriodCommand(int Year, int Month) : IRequest<PayrollPeriodActionResult>;

public class ApprovePayrollPeriodHandler(
    IPayrollRepository payrollRepository,
    IActivityLogService activityLogService,
    ICurrentUserService currentUser) : IRequestHandler<ApprovePayrollPeriodCommand, PayrollPeriodActionResult>
{
    public async Task<PayrollPeriodActionResult> Handle(ApprovePayrollPeriodCommand command, CancellationToken ct)
    {
        if (command.Month is < 1 or > 12)
            throw new ValidationException("Tháng phải nằm trong khoảng 1–12.");

        var records = await payrollRepository.GetByPeriodAsync(command.Year, command.Month, ct);

        var approved = 0;
        var skipped = 0;
        foreach (var record in records)
        {
            if (record.Status != PayrollStatus.Calculated)
            {
                skipped++;
                continue;
            }

            record.MarkApproved();
            approved++;
        }

        if (approved > 0)
            await payrollRepository.SaveChangesAsync(ct);

        await activityLogService.LogAsync(
            userId: currentUser.UserId,
            userName: currentUser.UserName,
            userRole: currentUser.UserRole,
            action: ActivityAction.Approve,
            module: ActivityModule.Payroll,
            description: $"Duyệt bảng lương tháng {command.Month}/{command.Year} cho {approved} nhân sự",
            status: ActivityStatus.Success,
            ipAddress: currentUser.IpAddress,
            targetId: $"{command.Year}-{command.Month:D2}",
            ct: ct);

        return new PayrollPeriodActionResult(approved, skipped, []);
    }
}
