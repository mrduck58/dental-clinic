using DentalClinic.API.Application.DTOs.Dashboard;
using DentalClinic.API.Application.Interfaces;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Dashboard;

public record GetServiceDistributionQuery(string? Range, int TopN) : IRequest<ServiceDistributionDto>;

public class GetServiceDistributionHandler(IDashboardQueryService dashboardQueryService)
    : IRequestHandler<GetServiceDistributionQuery, ServiceDistributionDto>
{
    public Task<ServiceDistributionDto> Handle(GetServiceDistributionQuery query, CancellationToken ct) =>
        dashboardQueryService.GetServiceDistributionAsync(query.Range, query.TopN, ct);
}
