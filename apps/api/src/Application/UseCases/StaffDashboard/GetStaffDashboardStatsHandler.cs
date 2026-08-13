using DentalClinic.API.Application.DTOs.StaffDashboard;
using DentalClinic.API.Application.Interfaces;
using MediatR;

namespace DentalClinic.API.Application.UseCases.StaffDashboard;

public record GetStaffDashboardStatsQuery : IRequest<StaffDashboardStatsDto>;

public class GetStaffDashboardStatsHandler(IStaffDashboardQueryService staffDashboardQueryService)
    : IRequestHandler<GetStaffDashboardStatsQuery, StaffDashboardStatsDto>
{
    public Task<StaffDashboardStatsDto> Handle(GetStaffDashboardStatsQuery query, CancellationToken ct) =>
        staffDashboardQueryService.GetStatsAsync(ct);
}
