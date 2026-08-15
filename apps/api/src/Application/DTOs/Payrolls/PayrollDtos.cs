namespace DentalClinic.API.Application.DTOs.Payrolls;

public record PayrollItemDto(
    Guid UserId,
    string FullName,
    string Email,
    string Role,
    string? EmployeeId,
    string? Department,
    string? Position,
    string? PhoneNumber,
    decimal BaseSalary,
    decimal Allowance,
    int LeaveDays,
    decimal AllowedLeaveDays,
    decimal ExceededDays,
    decimal Deduction,
    decimal Bonus,
    decimal NetSalary,
    string Status,
    DateTimeOffset? PaidAt,
    string? Note,
    // false = nhân sự chưa được thiết lập lương cơ bản trong hồ sơ
    bool HasSalaryConfigured,
    // Thực nhận của chính nhân sự này ở kỳ liền trước, để so sánh biến động
    decimal PreviousNetSalary,
    // true = kỳ đã có bản ghi (Draft trở lên). false = kỳ chưa được tạo, số liệu chỉ là ước tính.
    bool IsCreated);

public record PayrollSummaryDto(
    int TotalStaff,
    int PaidCount,
    int PendingCount,
    decimal TotalNet,
    decimal TotalPaid,
    decimal TotalDeduction,
    int MissingSalaryCount,
    decimal PreviousTotalNet,
    int NotCreatedCount,
    int DraftCount,
    int CalculatedCount,
    int ApprovedCount);

public record PayrollPeriodDto(
    int Year,
    int Month,
    int WorkingDaysPerMonth,
    PayrollSummaryDto Summary,
    IReadOnlyList<PayrollItemDto> Items);

// ── Báo cáo quỹ lương theo năm ───────────────────────────────────────────────

public record PayrollMonthStatDto(
    int Month,
    int StaffCount,
    int PaidCount,
    decimal TotalNet,
    decimal TotalPaid,
    decimal TotalDeduction);

public record PayrollYearlyDto(
    int Year,
    decimal TotalNet,
    decimal TotalPaid,
    decimal TotalDeduction,
    decimal AverageMonthlyNet,
    int PeakMonth,
    IReadOnlyList<PayrollMonthStatDto> Months);

// ── Bảng lương của tôi (Dentist/Staff tự xem) ────────────────────────────────

public record MyPayrollPeriodDto(
    int Year,
    int Month,
    int WorkingDaysPerMonth,
    PayrollItemDto? Item);

public record MyPayrollMonthDto(
    int Month,
    decimal NetSalary,
    string Status,
    DateTimeOffset? PaidAt);

public record MyPayrollYearlyDto(
    int Year,
    decimal TotalNet,
    int PaidCount,
    IReadOnlyList<MyPayrollMonthDto> Months);

// ── Requests / results ───────────────────────────────────────────────────────

public record PayPayrollRequest(int Year, int Month, Guid UserId, string? Note);

public record PayAllPayrollRequest(int Year, int Month, string? Note);

public record UnpayPayrollRequest(int Year, int Month, Guid UserId);

/// <summary>Một nhân sự không chi trả được trong đợt chi hàng loạt, kèm lý do cụ thể.</summary>
public record PayrollFailureDto(Guid UserId, string FullName, string Reason);

public record PayrollPeriodRequest(int Year, int Month);

public record SetPayrollBonusRequest(int Year, int Month, Guid UserId, decimal Bonus);

/// <summary>Kết quả một thao tác áp dụng cho cả kỳ (tạo/tính/duyệt) — số nhân sự bị bỏ qua kèm lý do.</summary>
public record PayrollPeriodActionResult(
    int AffectedCount,
    int SkippedCount,
    IReadOnlyList<PayrollFailureDto> Failures);
