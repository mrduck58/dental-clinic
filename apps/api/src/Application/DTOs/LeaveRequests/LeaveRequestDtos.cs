namespace DentalClinic.API.Application.DTOs.LeaveRequests;

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
    DateTimeOffset? ReviewedAt);

public record CreateLeaveRequestRequest(
    string LeaveType,
    DateOnly StartDate,
    DateOnly EndDate,
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
