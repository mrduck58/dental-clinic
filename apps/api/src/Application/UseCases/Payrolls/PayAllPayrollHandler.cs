using DentalClinic.API.Application.DTOs.Payrolls;
using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Payrolls;

/// <summary>Chi trả toàn bộ nhân sự ĐÃ DUYỆT (Approved) và chưa thanh toán của kỳ. Nhân sự chưa được
/// duyệt/chưa tạo kỳ lương bị bỏ qua thay vì làm hỏng cả lô.</summary>
public record PayAllPayrollCommand(int Year, int Month, string? Note) : IRequest<PayAllPayrollResult>;

/// <param name="AlreadyPaidCount">Đã chi trả từ trước — bỏ qua, không phải lỗi.</param>
/// <param name="Failures">Không chi trả được, kèm lý do để hiển thị riêng cho người dùng.</param>
public record PayAllPayrollResult(
    int PaidCount,
    int SkippedCount,
    decimal TotalPaid,
    int AlreadyPaidCount,
    IReadOnlyList<PayrollFailureDto> Failures);

public class PayAllPayrollHandler(
    IPayrollRepository payrollRepository,
    IActivityLogService activityLogService,
    ICurrentUserService currentUser) : IRequestHandler<PayAllPayrollCommand, PayAllPayrollResult>
{
    public async Task<PayAllPayrollResult> Handle(PayAllPayrollCommand command, CancellationToken ct)
    {
        if (command.Month is < 1 or > 12)
            throw new ValidationException("Tháng phải nằm trong khoảng 1–12.");

        var users = await payrollRepository.GetPayableUsersAsync(ct);
        var usersById = users.ToDictionary(u => u.Id);
        var records = await payrollRepository.GetByPeriodAsync(command.Year, command.Month, ct);

        var failures = new List<PayrollFailureDto>();
        var paidCount = 0;
        var alreadyPaidCount = 0;
        var totalPaid = 0m;

        foreach (var record in records)
        {
            if (record.Status == PayrollStatus.Paid)
            {
                alreadyPaidCount++;
                continue;
            }

            if (!usersById.TryGetValue(record.UserId, out var user))
                continue;

            try
            {
                PayPayrollHandler.MarkPaidOrThrow(user, record, command.Note);
            }
            catch (ValidationException ex)
            {
                // Ví dụ chưa được duyệt / chưa thiết lập lương → ghi lại lý do, không chặn cả đợt chi
                failures.Add(new PayrollFailureDto(user.Id, user.FullName, ex.Message));
                continue;
            }

            paidCount++;
            totalPaid += record.NetSalary;
        }

        if (paidCount > 0)
            await payrollRepository.SaveChangesAsync(ct);

        await activityLogService.LogAsync(
            userId: currentUser.UserId,
            userName: currentUser.UserName,
            userRole: currentUser.UserRole,
            action: ActivityAction.Payment,
            module: ActivityModule.Payroll,
            description: $"Chi trả lương tháng {command.Month}/{command.Year} cho {paidCount} nhân sự (tổng {totalPaid:N0} đ)"
                + (failures.Count > 0 ? $", {failures.Count} nhân sự không chi trả được" : string.Empty),
            status: failures.Count > 0 ? ActivityStatus.Warning : ActivityStatus.Success,
            ipAddress: currentUser.IpAddress,
            targetId: $"{command.Year}-{command.Month:D2}",
            ct: ct);

        return new PayAllPayrollResult(
            paidCount,
            SkippedCount: alreadyPaidCount + failures.Count,
            totalPaid,
            alreadyPaidCount,
            failures);
    }
}
