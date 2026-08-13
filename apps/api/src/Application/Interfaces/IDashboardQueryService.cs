using DentalClinic.API.Application.DTOs.Dashboard;

namespace DentalClinic.API.Application.Interfaces;

/// <summary>Read-model tổng hợp đa entity (Appointment, Invoice, Patient, Feedback, WorkSchedule...) cho nhóm Dashboard (admin).</summary>
public interface IDashboardQueryService
{
    Task<DashboardStatsDto> GetStatsAsync(string? range, CancellationToken ct);

    Task<AppointmentTrendDto> GetAppointmentTrendAsync(string? range, CancellationToken ct);

    Task<TodayAppointmentsDto> GetTodayAppointmentsAsync(int page, int pageSize, CancellationToken ct);

    Task<RecentFeedbackDto> GetRecentFeedbackAsync(int limit, CancellationToken ct);

    Task<ServiceDistributionDto> GetServiceDistributionAsync(string? range, int topN, CancellationToken ct);

    Task<WeeklyScheduleDto> GetWeeklyScheduleAsync(DateOnly? date, CancellationToken ct);
}
