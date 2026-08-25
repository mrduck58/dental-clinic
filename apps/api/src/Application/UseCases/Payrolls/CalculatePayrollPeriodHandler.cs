using DentalClinic.API.Application.DTOs.Payrolls;
using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Payrolls;

/// <summary>Tính lương: làm mới số liệu (theo hồ sơ + đơn nghỉ hiện tại, giữ nguyên Thưởng đã nhập) rồi
/// chốt các bản ghi đang Nháp của kỳ sang Đã tính. Bản ghi không ở trạng thái Nháp bị bỏ qua.</summary>
public record CalculatePayrollPeriodCommand(int Year, int Month) : IRequest<PayrollPeriodActionResult>;

public class CalculatePayrollPeriodHandler(
    IPayrollRepository payrollRepository,
    IWorkScheduleRepository workScheduleRepository,
    IActivityLogService activityLogService,
    ICurrentUserService currentUser) : IRequestHandler<CalculatePayrollPeriodCommand, PayrollPeriodActionResult>
{
    public async Task<PayrollPeriodActionResult> Handle(CalculatePayrollPeriodCommand command, CancellationToken ct)
    {
        if (command.Month is < 1 or > 12)
            throw new ValidationException("Tháng phải nằm trong khoảng 1–12.");

        var (from, to) = GetPayrollPeriodHandler.PeriodRange(command.Year, command.Month);
        var users = await payrollRepository.GetPayableUsersAsync(ct);
        var usersById = users.ToDictionary(u => u.Id);
        var records = await payrollRepository.GetByPeriodAsync(command.Year, command.Month, ct);
        var leaves = await payrollRepository.GetApprovedLeavesOverlappingAsync(from, to, ct);
        var shiftCounts = await PayrollShiftCounter.CountByEmployeeAsync(workScheduleRepository, from, to, ct);

        var calculated = 0;
        var skipped = 0;
        foreach (var record in records)
        {
            if (record.Status != PayrollStatus.Draft || !usersById.TryGetValue(record.UserId, out var user))
            {
                skipped++;
                continue;
            }

            var requiredShifts = shiftCounts.GetValueOrDefault(user.Employee?.Id ?? Guid.Empty, 0);
            var c = PayrollCalculator.Compute(user, leaves, requiredShifts, command.Year, command.Month);
            record.RefreshDraftFigures(c.BaseSalary, c.Allowance, c.RequiredShifts, c.LeaveShifts, c.AllowedLeaveShifts, c.ExceededShifts, c.Deduction);
            record.MarkCalculated();
            calculated++;
        }

        if (calculated > 0)
            await payrollRepository.SaveChangesAsync(ct);

        await activityLogService.LogAsync(
            userId: currentUser.UserId,
            userName: currentUser.UserName,
            userRole: currentUser.UserRole,
            action: ActivityAction.Edit,
            module: ActivityModule.Payroll,
            description: $"Tính lương tháng {command.Month}/{command.Year} cho {calculated} nhân sự",
            status: ActivityStatus.Success,
            ipAddress: currentUser.IpAddress,
            targetId: $"{command.Year}-{command.Month:D2}",
            ct: ct);

        return new PayrollPeriodActionResult(calculated, skipped, []);
    }
}
