using DentalClinic.API.Application.DTOs.Payrolls;
using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Enums;
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
        var record = await payrollRepository.GetByUserAndPeriodAsync(command.UserId, command.Year, command.Month, ct);

        MarkPaidOrThrow(user, record, command.Note);

        await payrollRepository.SaveChangesAsync(ct);

        await activityLogService.LogAsync(
            userId: currentUser.UserId,
            userName: currentUser.UserName,
            userRole: currentUser.UserRole,
            action: ActivityAction.Payment,
            module: ActivityModule.Payroll,
            description: $"Chi trả lương tháng {command.Month}/{command.Year} cho {user.FullName} ({record!.NetSalary:N0} đ)",
            status: ActivityStatus.Success,
            ipAddress: currentUser.IpAddress,
            targetId: record.Id.ToString(),
            ct: ct);

        return GetPayrollPeriodHandler.BuildItem(user, record, leaves, command.Year, command.Month);
    }

    /// <summary>Chi trả chỉ được phép khi kỳ lương của nhân sự này đã ở trạng thái Đã duyệt (Approved).</summary>
    internal static void MarkPaidOrThrow(Domain.Entities.User user, Domain.Entities.PayrollRecord? record, string? note)
    {
        if (record is null)
            throw new ValidationException(
                $"Kỳ lương của {user.FullName} chưa được tạo — hãy tạo, tính và duyệt kỳ lương trước khi chi trả.");

        if (record.Status != PayrollStatus.Approved)
            throw new ValidationException(
                $"Kỳ lương của {user.FullName} chưa được duyệt — chỉ có thể chi trả sau khi Owner duyệt bảng lương.");

        if (record.NetSalary <= 0)
            throw new ValidationException(
                $"Chưa thể chi trả cho {user.FullName}: nhân sự này chưa được thiết lập lương trong hồ sơ.");

        record.MarkPaid(note);
    }
}
