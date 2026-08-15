using DentalClinic.API.Application.DTOs.Payrolls;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Payrolls;

public record GetPayrollPeriodQuery(
    int Year,
    int Month,
    string? Search,
    string? Department,
    string? Role) : IRequest<PayrollPeriodDto>;

public class GetPayrollPeriodHandler(IPayrollRepository payrollRepository)
    : IRequestHandler<GetPayrollPeriodQuery, PayrollPeriodDto>
{
    public async Task<PayrollPeriodDto> Handle(GetPayrollPeriodQuery query, CancellationToken ct)
    {
        if (query.Month is < 1 or > 12)
            throw new ValidationException("Tháng phải nằm trong khoảng 1–12.");

        var (from, to) = PeriodRange(query.Year, query.Month);
        var (prevYear, prevMonth) = PreviousPeriod(query.Year, query.Month);
        var (prevFrom, _) = PeriodRange(prevYear, prevMonth);

        var users = await payrollRepository.GetPayableUsersAsync(ct);
        var records = await payrollRepository.GetByPeriodAsync(query.Year, query.Month, ct);
        var prevRecords = await payrollRepository.GetByPeriodAsync(prevYear, prevMonth, ct);
        // Một truy vấn phủ cả hai kỳ, tránh gọi DB hai lần chỉ để lấy số so sánh
        var leaves = await payrollRepository.GetApprovedLeavesOverlappingAsync(prevFrom, to, ct);

        var recordByUser = records.ToDictionary(r => r.UserId);
        var prevRecordByUser = prevRecords.ToDictionary(r => r.UserId);

        var items = users
            .Where(u => Matches(u, query))
            .Select(u => BuildItem(
                u,
                recordByUser.GetValueOrDefault(u.Id),
                leaves,
                query.Year,
                query.Month,
                previousNetSalary: NetSalaryOf(u, prevRecordByUser.GetValueOrDefault(u.Id), leaves, prevYear, prevMonth)))
            .ToList();

        var summary = new PayrollSummaryDto(
            TotalStaff: items.Count,
            PaidCount: items.Count(i => i.Status == nameof(PayrollStatus.Paid)),
            PendingCount: items.Count(i => i.Status != nameof(PayrollStatus.Paid)),
            TotalNet: items.Sum(i => i.NetSalary),
            TotalPaid: items.Where(i => i.Status == nameof(PayrollStatus.Paid)).Sum(i => i.NetSalary),
            TotalDeduction: items.Sum(i => i.Deduction),
            MissingSalaryCount: items.Count(i => !i.HasSalaryConfigured),
            PreviousTotalNet: items.Sum(i => i.PreviousNetSalary),
            NotCreatedCount: items.Count(i => i.Status == "NotCreated"),
            DraftCount: items.Count(i => i.Status == nameof(PayrollStatus.Draft)),
            CalculatedCount: items.Count(i => i.Status == nameof(PayrollStatus.Calculated)),
            ApprovedCount: items.Count(i => i.Status == nameof(PayrollStatus.Approved)));

        return new PayrollPeriodDto(
            query.Year,
            query.Month,
            PayrollCalculator.WorkingDaysPerMonth,
            summary,
            items);
    }

    internal static (DateOnly From, DateOnly To) PeriodRange(int year, int month)
    {
        var from = new DateOnly(year, month, 1);
        return (from, from.AddMonths(1).AddDays(-1));
    }

    internal static (int Year, int Month) PreviousPeriod(int year, int month)
        => month == 1 ? (year - 1, 12) : (year, month - 1);

    /// <summary>Thực nhận của một kỳ: đã có bản ghi (Draft trở lên) thì lấy số đã lưu, chưa tạo thì ước tính theo hồ sơ hiện tại.</summary>
    internal static decimal NetSalaryOf(
        User user, PayrollRecord? record, IEnumerable<LeaveRequest> approvedLeaves, int year, int month)
        => record is not null
            ? record.NetSalary
            : PayrollCalculator.Compute(user, approvedLeaves, year, month).NetSalary;

    /// <summary>
    /// Kỳ đã tạo (Draft trở lên) trả về đúng số liệu đã lưu — không âm thầm tính lại theo hồ sơ nữa,
    /// vì Draft có thể đã được sửa Thưởng tay. Kỳ CHƯA tạo hiển thị ước tính realtime (status "NotCreated").
    /// </summary>
    internal static PayrollItemDto BuildItem(
        User user,
        PayrollRecord? record,
        IEnumerable<LeaveRequest> approvedLeaves,
        int year,
        int month,
        decimal previousNetSalary = 0m)
    {
        var (employeeId, department, position) = ReadEmploymentProfile(user);

        if (record is not null)
        {
            return new PayrollItemDto(
                user.Id, user.FullName, user.Email ?? string.Empty, user.Role.ToString(),
                employeeId, department, position, user.PhoneNumber,
                record.BaseSalary, record.Allowance,
                record.LeaveDays, record.AllowedLeaveDays, record.ExceededDays,
                record.Deduction, record.Bonus, record.NetSalary,
                record.Status.ToString(), record.PaidAt, record.Note,
                HasSalaryConfigured: record.BaseSalary > 0,
                previousNetSalary,
                IsCreated: true);
        }

        var c = PayrollCalculator.Compute(user, approvedLeaves, year, month);

        return new PayrollItemDto(
            user.Id, user.FullName, user.Email ?? string.Empty, user.Role.ToString(),
            employeeId, department, position, user.PhoneNumber,
            c.BaseSalary, c.Allowance,
            c.LeaveDays, c.AllowedLeaveDays, c.ExceededDays,
            c.Deduction, 0m, c.NetSalary,
            "NotCreated", null, null,
            c.HasSalaryConfigured,
            previousNetSalary,
            IsCreated: false);
    }

    internal static (string? EmployeeId, string? Department, string? Position) ReadEmploymentProfile(User user)
        => (user.Employee?.EmployeeId, user.Employee?.Department, user.Employee?.Position);

    private static bool Matches(User user, GetPayrollPeriodQuery query)
    {
        var (employeeId, department, _) = ReadEmploymentProfile(user);

        if (!string.IsNullOrWhiteSpace(query.Role))
        {
            var roles = query.Role
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(r => Enum.TryParse<UserRole>(r, true, out _))
                .Select(r => Enum.Parse<UserRole>(r, true))
                .ToArray();
            if (!roles.Contains(user.Role))
                return false;
        }

        if (!string.IsNullOrWhiteSpace(query.Department)
            && !(department ?? string.Empty).Contains(query.Department, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            var haystack = string.Join(' ', user.FullName, user.Email, employeeId, user.PhoneNumber);
            if (!haystack.Contains(term, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }
}
