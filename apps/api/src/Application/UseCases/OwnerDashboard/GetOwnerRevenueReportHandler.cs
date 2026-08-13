using DentalClinic.API.Application.DTOs.OwnerDashboard;
using DentalClinic.API.Application.Interfaces;
using MediatR;

namespace DentalClinic.API.Application.UseCases.OwnerDashboard;

public record GetOwnerRevenueReportQuery(DateOnly? From, DateOnly? To) : IRequest<OwnerRevenueReportDto>;

public class GetOwnerRevenueReportHandler(IOwnerDashboardQueryService ownerDashboardQueryService)
    : IRequestHandler<GetOwnerRevenueReportQuery, OwnerRevenueReportDto>
{
    public async Task<OwnerRevenueReportDto> Handle(GetOwnerRevenueReportQuery request, CancellationToken cancellationToken)
    {
        return await ownerDashboardQueryService.GetRevenueReportAsync(request.From, request.To, cancellationToken);
    }
}
