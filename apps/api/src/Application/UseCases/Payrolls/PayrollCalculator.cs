using DentalClinic.API.Domain.Entities;

namespace DentalClinic.API.Application.UseCases.Payrolls;

public record PayrollComputation(
    decimal BaseSalary,
    decimal Allowance,
    int LeaveShifts,
    decimal AllowedLeaveShifts,
    decimal ExceededShifts,
    decimal Deduction,
    decimal NetSalary,
    bool HasSalaryConfigured);

/// <summary>
/// Quy tắc tính lương một kỳ:
///   - Lương cơ bản / phụ cấp / định mức phép (số ca) lấy từ hồ sơ nhân sự (Staff hoặc Dentist).
///     Chưa thiết lập thì coi như 0 — KHÔNG suy đoán một con số thay thế.
///   - Số ca nghỉ = số ngày của các đơn nghỉ ĐÃ DUYỆT rơi vào trong kỳ, quy đổi sang ca
///     theo <see cref="ShiftsPerDay"/> (đơn xin nghỉ vẫn khai theo khoảng ngày, chỉ quy đổi để so định mức).
///   - Vượt định mức bao nhiêu ca thì trừ bấy nhiêu ca công (lương cơ bản / <see cref="WorkingShiftsPerMonth"/>).
///   - Thực nhận = lương cơ bản + phụ cấp − khấu trừ.
/// </summary>
public static class PayrollCalculator
{
    /// <summary>Số ngày công quy ước trong tháng.</summary>
    public const int WorkingDaysPerMonth = 26;

    /// <summary>Số ca cố định trong một ngày (theo danh mục 6 ca của WorkShifts).</summary>
    public const int ShiftsPerDay = 6;

    /// <summary>Số ca công quy ước trong tháng (26 ngày × 6 ca/ngày), dùng để quy đổi lương một ca.</summary>
    public const int WorkingShiftsPerMonth = WorkingDaysPerMonth * ShiftsPerDay;

    public static (decimal? BaseSalary, decimal? Allowance, decimal? LeaveAccrued) ReadSalaryProfile(User user)
        => (user.Employee?.BaseSalary, user.Employee?.Allowance, user.Employee?.LeaveAccrued);

    /// <summary>Đếm số ngày của một đơn nghỉ rơi vào tháng/năm chỉ định.</summary>
    public static int CountLeaveDaysInPeriod(LeaveRequest leave, int year, int month)
    {
        var periodStart = new DateOnly(year, month, 1);
        var periodEnd = periodStart.AddMonths(1).AddDays(-1);

        var from = leave.StartDate > periodStart ? leave.StartDate : periodStart;
        var to = leave.EndDate < periodEnd ? leave.EndDate : periodEnd;

        return to < from ? 0 : to.DayNumber - from.DayNumber + 1;
    }

    public static PayrollComputation Compute(User user, IEnumerable<LeaveRequest> approvedLeaves, int year, int month)
    {
        var (profileBase, profileAllowance, profileLeaveAccrued) = ReadSalaryProfile(user);

        var baseSalary = profileBase ?? 0m;
        var allowance = profileAllowance ?? 0m;
        var allowedLeaveShifts = profileLeaveAccrued ?? 0m;

        var leaveDays = approvedLeaves
            .Where(l => l.UserId == user.Id)
            .Sum(l => CountLeaveDaysInPeriod(l, year, month));
        var leaveShifts = leaveDays * ShiftsPerDay;

        var exceededShifts = Math.Max(0m, leaveShifts - allowedLeaveShifts);
        var deduction = exceededShifts > 0
            ? Math.Round(exceededShifts * (baseSalary / WorkingShiftsPerMonth), 0, MidpointRounding.AwayFromZero)
            : 0m;

        // Khấu trừ không bao giờ vượt quá tổng thu nhập của kỳ (thực nhận không âm)
        deduction = Math.Min(deduction, baseSalary + allowance);
        var netSalary = baseSalary + allowance - deduction;

        return new PayrollComputation(
            baseSalary,
            allowance,
            leaveShifts,
            allowedLeaveShifts,
            exceededShifts,
            deduction,
            netSalary,
            HasSalaryConfigured: profileBase.HasValue);
    }
}
