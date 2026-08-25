using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;

namespace DentalClinic.API.Domain.Entities;

/// <summary>
/// Bảng lương của một nhân sự trong một kỳ (tháng/năm).
/// Vòng đời: Draft (mới tạo, còn sửa được kể cả Thưởng) → Calculated (đã tính, chốt số liệu —
/// muốn sửa phải tính lại, quay về Draft) → Approved (Owner đã duyệt) → Paid (đã thanh toán).
/// Chi trả chỉ được phép từ Approved; hoàn tác chi trả quay về Approved (không lùi về Draft).
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
    /// <summary>Số ca đã được phân lịch (WorkSchedule) trong kỳ — dùng để tính lương khi nhân sự không
    /// phải Full-time (BaseSalary khi đó là số ca này × đơn giá/ca), và để tham khảo khi Full-time.</summary>
    public int RequiredShifts { get; private set; }
    public int LeaveShifts { get; private set; }
    public decimal AllowedLeaveShifts { get; private set; }
    public decimal ExceededShifts { get; private set; }
    public decimal Deduction { get; private set; }
    public decimal Bonus { get; private set; }
    public decimal NetSalary { get; private set; }

    public PayrollStatus Status { get; private set; }
    public DateTimeOffset? PaidAt { get; private set; }
    public string? Note { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private PayrollRecord() { }

    /// <summary>Tạo kỳ lương ở trạng thái Nháp (Draft) — số liệu tính sẵn theo hồ sơ hiện tại nhưng còn sửa được.</summary>
    public static PayrollRecord CreateDraft(
        Guid userId,
        int year,
        int month,
        decimal baseSalary,
        decimal allowance,
        int requiredShifts,
        int leaveShifts,
        decimal allowedLeaveShifts,
        decimal exceededShifts,
        decimal deduction)
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
            RequiredShifts = requiredShifts,
            LeaveShifts = leaveShifts,
            AllowedLeaveShifts = allowedLeaveShifts,
            ExceededShifts = exceededShifts,
            Deduction = deduction,
            Bonus = 0m,
            NetSalary = baseSalary + allowance - deduction,
            Status = PayrollStatus.Draft,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    /// <summary>Sửa Thưởng của kỳ đang Nháp — chỉ khi chưa tính lương.</summary>
    public void SetBonus(decimal bonus)
    {
        if (Status != PayrollStatus.Draft)
            throw new ValidationException("Chỉ có thể sửa Thưởng khi kỳ lương đang ở trạng thái Nháp.");

        Bonus = bonus;
        NetSalary = BaseSalary + Allowance + Bonus - Deduction;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Tính lại số liệu lương/nghỉ phép theo hồ sơ hiện tại — chỉ khi còn ở trạng thái Nháp.</summary>
    public void RefreshDraftFigures(
        decimal baseSalary,
        decimal allowance,
        int requiredShifts,
        int leaveShifts,
        decimal allowedLeaveShifts,
        decimal exceededShifts,
        decimal deduction)
    {
        if (Status != PayrollStatus.Draft)
            throw new ValidationException("Chỉ có thể cập nhật số liệu khi kỳ lương đang ở trạng thái Nháp.");

        BaseSalary = baseSalary;
        Allowance = allowance;
        RequiredShifts = requiredShifts;
        LeaveShifts = leaveShifts;
        AllowedLeaveShifts = allowedLeaveShifts;
        ExceededShifts = exceededShifts;
        Deduction = deduction;
        NetSalary = baseSalary + allowance + Bonus - deduction;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Chốt số liệu — chuyển từ Nháp sang Đã tính.</summary>
    public void MarkCalculated()
    {
        if (Status != PayrollStatus.Draft)
            throw new ValidationException("Chỉ có thể tính lương từ trạng thái Nháp.");

        Status = PayrollStatus.Calculated;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Tính lại — đưa kỳ đã tính (chưa duyệt) quay về Nháp để sửa.</summary>
    public void ResetToDraft()
    {
        if (Status != PayrollStatus.Calculated)
            throw new ValidationException("Chỉ có thể tính lại kỳ lương đang ở trạng thái Đã tính.");

        Status = PayrollStatus.Draft;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Owner duyệt kỳ lương đã tính — đủ điều kiện chi trả.</summary>
    public void MarkApproved()
    {
        if (Status != PayrollStatus.Calculated)
            throw new ValidationException("Chỉ có thể duyệt kỳ lương đang ở trạng thái Đã tính.");

        Status = PayrollStatus.Approved;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkPaid(string? note = null)
    {
        if (Status != PayrollStatus.Approved)
            throw new ValidationException("Chỉ có thể chi trả kỳ lương đã được duyệt.");

        Status = PayrollStatus.Paid;
        PaidAt = DateTimeOffset.UtcNow;
        Note = note;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Hoàn tác chi trả — quay về Approved (không lùi về Draft), giữ nguyên số liệu đã chốt.</summary>
    public void MarkUnpaid()
    {
        if (Status != PayrollStatus.Paid)
            throw new ValidationException("Bảng lương này chưa được thanh toán.");

        Status = PayrollStatus.Approved;
        PaidAt = null;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
