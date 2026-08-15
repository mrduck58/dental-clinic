namespace DentalClinic.API.Domain.Entities;

public class WorkSchedule
{
    public Guid Id { get; private set; }
    public DateOnly Date { get; private set; }
    public string Shift { get; private set; } = "";       // "morning" | "afternoon"
    public string Type { get; private set; } = "";        // "dentist" | "staff"
    public string Role { get; private set; } = "";        // "dentist" | "assistant" | "staff"
    public string StaffName { get; private set; } = "";
    public string Room { get; private set; } = "";
    public string RoomColor { get; private set; } = "";   // Tailwind border class e.g. "border-primary"
    public bool IsHoliday { get; private set; }

    public Guid? EmployeeId { get; private set; }
    public Guid? RoomId { get; private set; }
    public Employee? Employee { get; private set; }
    public Room? RoomNavigation { get; private set; }

    private WorkSchedule() { }

    /// <param name="employeeId">
    /// Nhân sự được phân ca. Đây mới là mối nối THẬT giữa dòng lịch và con người —
    /// <see cref="StaffName"/> chỉ là chuỗi hiển thị, viết mỗi nơi một kiểu ("Đỗ Văn Phong" ở đây,
    /// "BS.Đỗ Văn Phong" trong hồ sơ) nên không dò theo nó được. Cho phép null vì dòng nhập từ
    /// Excel có thể ghi một cái tên không khớp ai trong hệ thống; những dòng đó vẫn phải lưu được
    /// để người xếp lịch nhìn thấy và sửa, chứ không âm thầm biến mất.
    /// </param>
    public static WorkSchedule Create(
        DateOnly date, string shift, string type, string role,
        string staffName, string room, string roomColor, bool isHoliday,
        Guid? employeeId = null) => new()
    {
        Id = Guid.NewGuid(),
        Date = date,
        Shift = shift,
        Type = type,
        Role = role,
        StaffName = staffName,
        Room = room,
        RoomColor = roomColor,
        IsHoliday = isHoliday,
        EmployeeId = employeeId
    };
}
