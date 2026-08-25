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
    // "Full-time" (hoặc chưa thiết lập) = lương tháng + khấu trừ phép. Khác thì = số ca × đơn giá/ca.
    string? EmploymentType,
    decimal BaseSalary,
    decimal Allowance,
    // Số ca đã được phân lịch (WorkSchedule) trong kỳ — dùng để tính lương khi không phải Full-time
    // (BaseSalary khi đó = số ca này × đơn giá/ca), chỉ mang tính tham khảo khi Full-time.
    int RequiredShifts,
    int LeaveShifts,
    decimal AllowedLeaveShifts,
    decimal ExceededShifts,
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
    int WorkingShiftsPerMonth,
    PayrollSummaryDto Summary,
    IReadOnlyList<PayrollItemDto> Items);

// ── Bảng lương của tôi (Dentist/Staff tự xem) ────────────────────────────────

public record MyPayrollPeriodDto(
    int Year,
    int Month,
    int WorkingShiftsPerMonth,
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
