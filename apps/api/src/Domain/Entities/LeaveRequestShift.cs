namespace DentalClinic.API.Domain.Entities;

/// <summary>Một ca cụ thể (ngày + mã ca) mà người nộp đơn muốn nghỉ — xem <see cref="LeaveRequest.Shifts"/>.</summary>
public class LeaveRequestShift
{
    public Guid Id { get; private set; }
    public Guid LeaveRequestId { get; private set; }
    public DateOnly Date { get; private set; }
    public string ShiftId { get; private set; } = string.Empty;

    private LeaveRequestShift() { }

    internal static LeaveRequestShift Create(DateOnly date, string shiftId) => new()
    {
        Id = Guid.NewGuid(),
        Date = date,
        ShiftId = shiftId,
    };
}
