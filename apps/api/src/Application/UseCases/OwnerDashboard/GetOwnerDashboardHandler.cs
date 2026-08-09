using DentalClinic.API.Application.DTOs.OwnerDashboard;
using DentalClinic.API.Application.Interfaces;
using MediatR;

namespace DentalClinic.API.Application.UseCases.OwnerDashboard;

public record GetOwnerDashboardQuery() : IRequest<OwnerDashboardDto>;

public class GetOwnerDashboardHandler(IOwnerDashboardQueryService ownerDashboardQueryService)
    : IRequestHandler<GetOwnerDashboardQuery, OwnerDashboardDto>
{
    public async Task<OwnerDashboardDto> Handle(GetOwnerDashboardQuery request, CancellationToken cancellationToken)
    {
        return await ownerDashboardQueryService.GetOwnerDashboardAsync(cancellationToken);
    }
}
