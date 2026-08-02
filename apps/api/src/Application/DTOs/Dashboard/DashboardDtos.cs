namespace DentalClinic.API.Application.DTOs.Dashboard;

/// <summary>3 chỉ số chính của dashboard (bệnh nhân mới, lịch hẹn, doanh thu) so với kỳ trước đó.</summary>
public record DashboardStatsDto(
    string Range,
    DateTimeOffset PeriodStart,
    DateTimeOffset PeriodEnd,
    int NewPatientsCount,
    double NewPatientsTrendPercent,
    int AppointmentsCount,
    double AppointmentsTrendPercent,
    decimal RevenueAmount,
    double RevenueTrendPercent);

public record AppointmentTrendPointDto(DateTimeOffset PeriodStart, DateTimeOffset PeriodEnd, int Count);

public record AppointmentTrendDto(string Range, IReadOnlyList<AppointmentTrendPointDto> Points);

/// <summary>ServiceId/ServiceName null đại diện cho nhóm "các dịch vụ còn lại" khi vượt quá topN.</summary>
public record ServiceDistributionItemDto(Guid? ServiceId, string? ServiceName, int Count, double Percentage);

public record ServiceDistributionDto(string Range, int TotalAppointments, IReadOnlyList<ServiceDistributionItemDto> Items);

public record TodayAppointmentItemDto(
    Guid Id,
    DateTimeOffset AppointmentDate,
    string PatientName,
    string? ServiceName,
    string Status);

public record TodayAppointmentsDto(
    IReadOnlyList<TodayAppointmentItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);

public record CalendarDayDto(DateOnly Date, bool IsToday);

public record ShiftEntryDto(
    string StaffName,
    string? Specialization,
    string? ProfilePictureUrl,
    string Room,
    string RoomColor,
    bool IsBusy);

public record WeeklyScheduleDto(
    DateOnly SelectedDate,
    IReadOnlyList<CalendarDayDto> Week,
    IReadOnlyList<ShiftEntryDto> MorningShift,
    IReadOnlyList<ShiftEntryDto> AfternoonShift);

public record FeedbackSummaryDto(Guid Id, string CustomerName, int Rating, string Comment, DateTimeOffset CreatedAt);

public record RecentFeedbackDto(
    IReadOnlyList<FeedbackSummaryDto> Items,
    double AverageRating,
    int TotalFeaturedCount);
