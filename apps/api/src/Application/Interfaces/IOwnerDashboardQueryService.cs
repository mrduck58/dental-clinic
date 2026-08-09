using DentalClinic.API.Application.DTOs.OwnerDashboard;

namespace DentalClinic.API.Application.Interfaces;

public interface IOwnerDashboardQueryService
{
    Task<OwnerDashboardDto> GetOwnerDashboardAsync(CancellationToken ct);
}
