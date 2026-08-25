namespace DentalClinic.API.Application.DTOs.LeaveRequests;

public record LeaveRequestShiftDto(DateOnly Date, string ShiftId);

public record LeaveRequestDto(
    Guid Id,
    Guid UserId,
    string UserFullName,
    string? Department,
    string LeaveType,
    DateOnly StartDate,
    DateOnly EndDate,
    int DaysCount,
    string Reason,
    string Status,
    string? ReviewerNote,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReviewedAt,
    IReadOnlyList<LeaveRequestShiftDto> Shifts);

public record LeaveRequestShiftInput(DateOnly Date, string ShiftId);

public record CreateLeaveRequestRequest(
    string LeaveType,
    IReadOnlyList<LeaveRequestShiftInput> Shifts,
    string Reason);

public record RejectLeaveRequestRequest(
    string? ReviewerNote);

public record MyLeaveStatsDto(
    int TotalAnnualDays,
    int UsedAnnualDays,
    int RemainingAnnualDays,
    int PendingCount,
    int ApprovedThisYear);

public record MyLeaveRequestsResponse(
    MyLeaveStatsDto Stats,
    IEnumerable<LeaveRequestDto> Requests);

// ── Ảnh hưởng của đơn nghỉ ───────────────────────────────────────────────────
// Lịch làm việc gắn nhân sự bằng TÊN (WorkSchedules.StaffName), không bằng khóa ngoại —
// SaveWeekScheduleHandler chỉ ghi tên. Nên mọi phép đối chiếu "đơn nghỉ này đụng ca nào"
// đều so khớp theo tên hiển thị của người nộp đơn.

/// <summary>Một ca làm việc trùng vào khoảng ngày xin nghỉ — sẽ bị gỡ khỏi lịch nếu đơn được duyệt.</summary>
public record LeaveImpactShiftDto(
    Guid ScheduleId,
    string Shift,
    string Room,
    string Role,
    string Type);

/// <summary>Ảnh hưởng trong một ngày cụ thể: các ca bị đụng + số lịch hẹn đã đặt của bác sĩ ngày đó.</summary>
public record LeaveImpactDayDto(
    DateOnly Date,
    IReadOnlyList<LeaveImpactShiftDto> Shifts,
    int AppointmentCount,
    IReadOnlyList<string> AppointmentTimes);

public record LeaveImpactDto(
    Guid LeaveRequestId,
    string StaffName,
    string Status,
    DateOnly StartDate,
    DateOnly EndDate,
    int AffectedDayCount,
    int AffectedShiftCount,
    int AffectedAppointmentCount,
    IReadOnlyList<LeaveImpactDayDto> Days);

/// <summary>
/// Kết quả duyệt đơn: ngoài đơn đã cập nhật còn kèm số ca vừa bị gỡ khỏi lịch làm việc,
/// để màn hình Owner báo lại ngay cần bổ sung bao nhiêu ca và ở tuần nào.
/// </summary>
public record ApproveLeaveRequestResultDto(
    LeaveRequestDto Request,
    int RemovedShiftCount,
    int AffectedDayCount,
    int AffectedAppointmentCount,
    IReadOnlyList<DateOnly> AffectedDates);
