using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;

namespace DentalClinic.API.Domain.Entities;

/// <summary>
/// Bảng lương của một nhân sự trong một kỳ (tháng/năm).
/// Các con số lương được chốt (snapshot) tại thời điểm ghi nhận, không đọc lại từ hồ sơ nhân sự,
/// để bảng lương các tháng cũ không bị thay đổi khi lương nhân sự được điều chỉnh về sau.
/// </summary>
public class PayrollRecord
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;
    public int Year { get; private set; }
    public int Month { get; private set; }

    public decimal BaseSalary { get; private set; }
    public decimal Allowance { get; private set; }
    public int LeaveDays { get; private set; }
    public decimal AllowedLeaveDays { get; private set; }
    public decimal ExceededDays { get; private set; }
    public decimal Deduction { get; private set; }
    public decimal NetSalary { get; private set; }

    public PayrollStatus Status { get; private set; }
    public DateTimeOffset? PaidAt { get; private set; }
    public string? Note { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private PayrollRecord() { }

    public static PayrollRecord Create(
        Guid userId,
        int year,
        int month,
        decimal baseSalary,
        decimal allowance,
        int leaveDays,
        decimal allowedLeaveDays,
        decimal exceededDays,
        decimal deduction,
        decimal netSalary)
    {
        if (month is < 1 or > 12)
            throw new ValidationException("Tháng của kỳ lương phải nằm trong khoảng 1–12.");
        if (year is < 2000 or > 2200)
            throw new ValidationException("Năm của kỳ lương không hợp lệ.");

        var now = DateTimeOffset.UtcNow;
        return new PayrollRecord
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Year = year,
            Month = month,
            BaseSalary = baseSalary,
            Allowance = allowance,
            LeaveDays = leaveDays,
            AllowedLeaveDays = allowedLeaveDays,
            ExceededDays = exceededDays,
            Deduction = deduction,
            NetSalary = netSalary,
            Status = PayrollStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    /// <summary>Cập nhật lại các con số theo hồ sơ nhân sự / đơn nghỉ hiện tại. Chỉ cho phép khi chưa chi trả.</summary>
    public void Recalculate(
        decimal baseSalary,
        decimal allowance,
        int leaveDays,
        decimal allowedLeaveDays,
        decimal exceededDays,
        decimal deduction,
        decimal netSalary)
    {
        if (Status == PayrollStatus.Paid)
            throw new ValidationException("Không thể tính lại bảng lương đã thanh toán.");

        BaseSalary = baseSalary;
        Allowance = allowance;
        LeaveDays = leaveDays;
        AllowedLeaveDays = allowedLeaveDays;
        ExceededDays = exceededDays;
        Deduction = deduction;
        NetSalary = netSalary;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkPaid(string? note = null)
    {
        if (Status == PayrollStatus.Paid)
            throw new ValidationException("Bảng lương này đã được thanh toán.");

        Status = PayrollStatus.Paid;
        PaidAt = DateTimeOffset.UtcNow;
        Note = note;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkUnpaid()
    {
        if (Status != PayrollStatus.Paid)
            throw new ValidationException("Bảng lương này chưa được thanh toán.");

        Status = PayrollStatus.Pending;
        PaidAt = null;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
