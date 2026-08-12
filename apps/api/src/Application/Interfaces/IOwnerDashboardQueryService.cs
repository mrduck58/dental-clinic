using DentalClinic.API.Application.DTOs.OwnerDashboard;

namespace DentalClinic.API.Application.Interfaces;

public interface IOwnerDashboardQueryService
{
    Task<OwnerDashboardDto> GetOwnerDashboardAsync(CancellationToken ct);

    /// <summary>Báo cáo thu/chi chi tiết theo từng khoản, lọc theo khoảng thời gian (week/month/year/all).</summary>
    Task<OwnerRevenueReportDto> GetRevenueReportAsync(DateOnly? from, DateOnly? to, CancellationToken ct);
}
