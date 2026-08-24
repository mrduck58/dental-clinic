using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;

namespace DentalClinic.API.Domain.Entities;

public class LeaveRequest
{
    private readonly List<LeaveRequestShift> _shifts = [];

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;
    public LeaveType LeaveType { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public int DaysCount { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public LeaveStatus Status { get; private set; }
    public string? ReviewerNote { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ReviewedAt { get; private set; }

    /// <summary>Các ca cụ thể (ngày + mã ca) người nộp đơn muốn nghỉ. StartDate/EndDate/DaysCount
    /// đều suy ra từ tập này — xem <see cref="Create"/>.</summary>
    public IReadOnlyList<LeaveRequestShift> Shifts => _shifts;

    private LeaveRequest() { }

    public static LeaveRequest Create(
        Guid userId,
        LeaveType leaveType,
        IReadOnlyList<(DateOnly Date, string ShiftId)> shifts,
        string reason)
    {
        var distinctShifts = shifts
            .Distinct()
            .OrderBy(s => s.Date)
            .ThenBy(s => s.ShiftId)
            .ToList();

        if (distinctShifts.Count == 0)
            throw new ValidationException("Vui lòng chọn ít nhất một ca muốn nghỉ.");

        var request = new LeaveRequest
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            LeaveType = leaveType,
            StartDate = distinctShifts[0].Date,
            EndDate = distinctShifts[^1].Date,
            DaysCount = distinctShifts.Select(s => s.Date).Distinct().Count(),
            Reason = reason,
            Status = LeaveStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        request._shifts.AddRange(distinctShifts.Select(s => LeaveRequestShift.Create(s.Date, s.ShiftId)));

        return request;
    }

    public void Approve()
    {
        if (Status != LeaveStatus.Pending)
            throw new ValidationException("Chỉ có thể duyệt đơn đang chờ xử lý.");

        Status = LeaveStatus.Approved;
        ReviewedAt = DateTimeOffset.UtcNow;
    }

    public void Reject(string? note)
    {
        if (Status != LeaveStatus.Pending)
            throw new ValidationException("Chỉ có thể từ chối đơn đang chờ xử lý.");

        Status = LeaveStatus.Rejected;
        ReviewerNote = note;
        ReviewedAt = DateTimeOffset.UtcNow;
    }

    public void Cancel()
    {
        if (Status != LeaveStatus.Pending)
            throw new ValidationException("Chỉ có thể hủy đơn đang chờ xử lý.");

        Status = LeaveStatus.Cancelled;
    }
}
