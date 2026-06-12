namespace DentalClinic.API.Application.DTOs.Schedules;

public record ScheduleEntryDto(
    string Id,
    string Date,       // "YYYY-MM-DD"
    string Shift,      // "morning" | "afternoon"
    string Type,       // "dentist" | "staff"
    string Role,       // "dentist" | "assistant" | "staff"
    string Name,
    string Room,
    string RoomColor,
    bool IsHoliday
);

public record SaveScheduleEntryRequest(
    string Date,
    string Shift,
    string Type,
    string Role,
    string Name,
    string Room,
    string RoomColor,
    bool IsHoliday
);

public record SaveWeekScheduleRequest(IEnumerable<SaveScheduleEntryRequest> Entries);
