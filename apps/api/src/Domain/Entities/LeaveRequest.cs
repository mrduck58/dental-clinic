using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;

namespace DentalClinic.API.Domain.Entities;

public class LeaveRequest
{
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

    private LeaveRequest() { }

    public static LeaveRequest Create(
        Guid userId,
        LeaveType leaveType,
        DateOnly startDate,
        DateOnly endDate,
        string reason)
    {
        if (endDate < startDate)
            throw new ValidationException("Ngày kết thúc phải sau hoặc bằng ngày bắt đầu.");

        return new LeaveRequest
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            LeaveType = leaveType,
            StartDate = startDate,
            EndDate = endDate,
            DaysCount = endDate.DayNumber - startDate.DayNumber + 1,
            Reason = reason,
            Status = LeaveStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
        };
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
