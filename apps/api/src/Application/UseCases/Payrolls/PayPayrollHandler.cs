using DentalClinic.API.Application.DTOs.Payrolls;
using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Payrolls;

public record PayPayrollCommand(int Year, int Month, Guid UserId, string? Note) : IRequest<PayrollItemDto>;

public class PayPayrollHandler(
    IPayrollRepository payrollRepository,
    IActivityLogService activityLogService,
    ICurrentUserService currentUser) : IRequestHandler<PayPayrollCommand, PayrollItemDto>
{
    public async Task<PayrollItemDto> Handle(PayPayrollCommand command, CancellationToken ct)
    {
        if (command.Month is < 1 or > 12)
            throw new ValidationException("Tháng phải nằm trong khoảng 1–12.");

        var (from, to) = GetPayrollPeriodHandler.PeriodRange(command.Year, command.Month);

        var users = await payrollRepository.GetPayableUsersAsync(ct);
        var user = users.FirstOrDefault(u => u.Id == command.UserId)
            ?? throw new NotFoundException($"Không tìm thấy nhân sự với ID: {command.UserId}");

        var leaves = await payrollRepository.GetApprovedLeavesOverlappingAsync(from, to, ct);
        var existing = await payrollRepository.GetByUserAndPeriodAsync(command.UserId, command.Year, command.Month, ct);

        var (record, isNew) = BuildPaidRecord(user, existing, leaves, command.Year, command.Month, command.Note);

        if (isNew)
            await payrollRepository.AddAsync(record, ct);
        else
            await payrollRepository.SaveChangesAsync(ct);

        await activityLogService.LogAsync(
            userId: currentUser.UserId,
            userName: currentUser.UserName,
            userRole: currentUser.UserRole,
            action: ActivityAction.Payment,
            module: ActivityModule.Payroll,
            description: $"Chi trả lương tháng {command.Month}/{command.Year} cho {user.FullName} ({record.NetSalary:N0} đ)",
            status: ActivityStatus.Success,
            ipAddress: currentUser.IpAddress,
            targetId: record.Id.ToString(),
            ct: ct);

        return GetPayrollPeriodHandler.BuildItem(user, record, leaves, command.Year, command.Month);
    }

    /// <summary>
    /// Chốt lại các con số của kỳ theo hồ sơ lương hiện tại rồi đánh dấu đã chi trả.
    /// Chưa ghi xuống DB — người gọi tự quyết định thời điểm lưu (pay-all lưu một lần cho cả lô).
    /// </summary>
    internal static (PayrollRecord Record, bool IsNew) BuildPaidRecord(
        User user,
        PayrollRecord? existing,
        IReadOnlyList<LeaveRequest> approvedLeaves,
        int year,
        int month,
        string? note)
    {
        if (existing is { Status: Domain.Enums.PayrollStatus.Paid })
            throw new ValidationException($"Lương tháng {month}/{year} của {user.FullName} đã được thanh toán.");

        var c = PayrollCalculator.Compute(user, approvedLeaves, year, month);

        if (c.NetSalary <= 0)
            throw new ValidationException(
                $"Chưa thể chi trả cho {user.FullName}: nhân sự này chưa được thiết lập lương trong hồ sơ.");

        if (existing is null)
        {
            var record = PayrollRecord.Create(
                user.Id, year, month,
                c.BaseSalary, c.Allowance, c.LeaveDays, c.AllowedLeaveDays,
                c.ExceededDays, c.Deduction, c.NetSalary);
            record.MarkPaid(note);
            return (record, true);
        }

        existing.Recalculate(
            c.BaseSalary, c.Allowance, c.LeaveDays, c.AllowedLeaveDays,
            c.ExceededDays, c.Deduction, c.NetSalary);
        existing.MarkPaid(note);
        return (existing, false);
    }
}
