using DentalClinic.API.Domain.Entities;

namespace DentalClinic.API.Application.UseCases.Payrolls;

public record PayrollComputation(
    decimal BaseSalary,
    decimal Allowance,
    int LeaveDays,
    decimal AllowedLeaveDays,
    decimal ExceededDays,
    decimal Deduction,
    decimal NetSalary,
    bool HasSalaryConfigured);

/// <summary>
/// Quy tắc tính lương một kỳ:
///   - Lương cơ bản / phụ cấp / định mức phép lấy từ hồ sơ nhân sự (Staff hoặc Dentist).
///     Chưa thiết lập thì coi như 0 — KHÔNG suy đoán một con số thay thế.
///   - Số ngày nghỉ = số ngày của các đơn nghỉ ĐÃ DUYỆT rơi vào trong kỳ.
///   - Vượt định mức bao nhiêu ngày thì trừ bấy nhiêu ngày công (lương cơ bản / 26).
///   - Thực nhận = lương cơ bản + phụ cấp − khấu trừ.
/// </summary>
public static class PayrollCalculator
{
    /// <summary>Số ngày công quy ước trong tháng, dùng để quy đổi lương một ngày.</summary>
    public const int WorkingDaysPerMonth = 26;

    public static (decimal? BaseSalary, decimal? Allowance, decimal? LeaveAccrued) ReadSalaryProfile(User user)
    {
        if (user.Role is "Dentist" or "Doctor")
            return (user.Dentist?.BaseSalary, user.Dentist?.Allowance, user.Dentist?.LeaveAccrued);

        return (user.Staff?.BaseSalary, user.Staff?.Allowance, user.Staff?.LeaveAccrued);
    }

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
        var allowedLeaveDays = profileLeaveAccrued ?? 0m;

        var leaveDays = approvedLeaves
            .Where(l => l.UserId == user.Id)
            .Sum(l => CountLeaveDaysInPeriod(l, year, month));

        var exceededDays = Math.Max(0m, leaveDays - allowedLeaveDays);
        var deduction = exceededDays > 0
            ? Math.Round(exceededDays * (baseSalary / WorkingDaysPerMonth), 0, MidpointRounding.AwayFromZero)
            : 0m;

        // Khấu trừ không bao giờ vượt quá tổng thu nhập của kỳ (thực nhận không âm)
        deduction = Math.Min(deduction, baseSalary + allowance);
        var netSalary = baseSalary + allowance - deduction;

        return new PayrollComputation(
            baseSalary,
            allowance,
            leaveDays,
            allowedLeaveDays,
            exceededDays,
            deduction,
            netSalary,
            HasSalaryConfigured: profileBase.HasValue);
    }
}
