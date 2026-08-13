using DentalClinic.API.Application.DTOs.StaffDashboard;

namespace DentalClinic.API.Application.Interfaces;

/// <summary>Read-model tổng hợp đa entity (Appointment, Invoice) cho nhóm StaffDashboard.</summary>
public interface IStaffDashboardQueryService
{
    Task<StaffDashboardStatsDto> GetStatsAsync(CancellationToken ct);

    Task<IReadOnlyList<StaffTodayAppointmentDto>> GetTodayAppointmentsAsync(int limit, CancellationToken ct);

    Task<IReadOnlyList<StaffPendingInvoiceDto>> GetPendingInvoicesAsync(int limit, CancellationToken ct);
}
