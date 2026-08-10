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

    public static WorkSchedule Create(
        DateOnly date, string shift, string type, string role,
        string staffName, string room, string roomColor, bool isHoliday) => new()
    {
        Id = Guid.NewGuid(),
        Date = date,
        Shift = shift,
        Type = type,
        Role = role,
        StaffName = staffName,
        Room = room,
        RoomColor = roomColor,
        IsHoliday = isHoliday
    };
}
