using DentalClinic.API.Domain.Entities;

namespace DentalClinic.API.Application.UseCases.Payrolls;

public record PayrollComputation(
    decimal BaseSalary,
    decimal Allowance,
    int RequiredShifts,
    int LeaveShifts,
    decimal AllowedLeaveShifts,
    decimal ExceededShifts,
    decimal Deduction,
    decimal NetSalary,
    bool HasSalaryConfigured);

/// <summary>
/// Quy tắc tính lương một kỳ — có 2 công thức tách biệt theo hình thức làm việc
/// (<see cref="Domain.Entities.Employee.EmploymentType"/>):
///
///   - "Full-time": lương tháng cố định + trừ nếu nghỉ vượt định mức.
///       Số ca nghỉ = số ngày của các đơn nghỉ ĐÃ DUYỆT rơi vào trong kỳ, quy đổi sang ca theo
///       <see cref="ShiftsPerDay"/> (đơn xin nghỉ vẫn khai theo khoảng ngày, chỉ quy đổi để so định mức).
///       Vượt định mức bao nhiêu ca thì trừ bấy nhiêu ca công (lương cơ bản / <see cref="WorkingShiftsPerMonth"/>).
///       Thực nhận = lương cơ bản + phụ cấp − khấu trừ.
///   - Khác "Full-time" (Part-time, Shift-based, ...): KHÔNG áp dụng lương tháng/khấu trừ phép — lương
///       hoàn toàn theo số ca thực tế đã được phân lịch trong kỳ (<see cref="RequiredShifts"/>, đếm từ
///       WorkSchedule) nhân với đơn giá/ca (<see cref="Domain.Entities.Employee.RatePerShift"/>).
///       Thực nhận = (số ca × đơn giá/ca) + phụ cấp.
///
/// Lương cơ bản / phụ cấp / định mức phép / đơn giá ca lấy từ hồ sơ nhân sự. Chưa thiết lập thì coi như 0
/// — KHÔNG suy đoán một con số thay thế.
/// </summary>
public static class PayrollCalculator
{
    /// <summary>Số ngày công quy ước trong tháng.</summary>
    public const int WorkingDaysPerMonth = 26;

    /// <summary>Số ca cố định trong một ngày (theo danh mục 6 ca của WorkShifts) — chỉ dùng để quy đổi
    /// ngày nghỉ phép sang ca cho nhân sự Full-time.</summary>
    public const int ShiftsPerDay = 6;

    /// <summary>Số ca công quy ước trong tháng (26 ngày × 6 ca/ngày), dùng để quy đổi lương một ca cho
    /// nhân sự Full-time.</summary>
    public const int WorkingShiftsPerMonth = WorkingDaysPerMonth * ShiftsPerDay;

    /// <summary>Chưa thiết lập hình thức làm việc thì coi như Full-time (hành vi trước khi có công thức
    /// theo ca, không âm thầm đổi cách tính lương của những hồ sơ cũ chưa cập nhật EmploymentType).</summary>
    public static bool IsFullTime(User user)
    {
        var employmentType = user.Employee?.EmploymentType;
        return employmentType is null || string.Equals(employmentType, "Full-time", StringComparison.OrdinalIgnoreCase);
    }

    public static (decimal? BaseSalary, decimal? Allowance, decimal? LeaveAccrued, decimal? RatePerShift) ReadSalaryProfile(User user)
        => (user.Employee?.BaseSalary, user.Employee?.Allowance, user.Employee?.LeaveAccrued, user.Employee?.RatePerShift);

    /// <summary>Đếm số ngày của một đơn nghỉ rơi vào tháng/năm chỉ định.</summary>
    public static int CountLeaveDaysInPeriod(LeaveRequest leave, int year, int month)
    {
        var periodStart = new DateOnly(year, month, 1);
        var periodEnd = periodStart.AddMonths(1).AddDays(-1);

        var from = leave.StartDate > periodStart ? leave.StartDate : periodStart;
        var to = leave.EndDate < periodEnd ? leave.EndDate : periodEnd;

        return to < from ? 0 : to.DayNumber - from.DayNumber + 1;
    }

    /// <param name="requiredShifts">Số ca đã được phân lịch (WorkSchedule) cho nhân sự này trong kỳ —
    /// chỉ có ý nghĩa tính lương khi KHÔNG phải Full-time; với Full-time đây chỉ là số liệu hiển thị.</param>
    public static PayrollComputation Compute(
        User user, IEnumerable<LeaveRequest> approvedLeaves, int requiredShifts, int year, int month)
    {
        var (profileBase, profileAllowance, profileLeaveAccrued, profileRatePerShift) = ReadSalaryProfile(user);
        var allowance = profileAllowance ?? 0m;

        if (!IsFullTime(user))
        {
            var ratePerShift = profileRatePerShift ?? 0m;
            var shiftPay = requiredShifts * ratePerShift;
            var netSalaryShiftBased = shiftPay + allowance;

            return new PayrollComputation(
                BaseSalary: shiftPay,
                Allowance: allowance,
                RequiredShifts: requiredShifts,
                LeaveShifts: 0,
                AllowedLeaveShifts: 0m,
                ExceededShifts: 0m,
                Deduction: 0m,
                NetSalary: netSalaryShiftBased,
                HasSalaryConfigured: profileRatePerShift.HasValue);
        }

        var baseSalary = profileBase ?? 0m;
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
            requiredShifts,
            leaveShifts,
            allowedLeaveShifts,
            exceededShifts,
            deduction,
            netSalary,
            HasSalaryConfigured: profileBase.HasValue);
    }
}
