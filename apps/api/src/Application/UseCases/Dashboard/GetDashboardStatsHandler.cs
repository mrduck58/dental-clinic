using DentalClinic.API.Application.DTOs.Dashboard;
using DentalClinic.API.Application.Interfaces;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Dashboard;

public record GetDashboardStatsQuery(string? Range) : IRequest<DashboardStatsDto>;

public class GetDashboardStatsHandler(IDashboardQueryService dashboardQueryService)
    : IRequestHandler<GetDashboardStatsQuery, DashboardStatsDto>
{
    public Task<DashboardStatsDto> Handle(GetDashboardStatsQuery query, CancellationToken ct) =>
        dashboardQueryService.GetStatsAsync(query.Range, ct);
}
