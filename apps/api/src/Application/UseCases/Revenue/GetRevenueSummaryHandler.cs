using DentalClinic.API.Application.DTOs.Revenue;
using DentalClinic.API.Application.Interfaces;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Revenue;

public record GetRevenueSummaryQuery(DateOnly From, DateOnly To) : IRequest<RevenueSummaryDto>;

public class GetRevenueSummaryHandler(IRevenueQueryService revenueQueryService)
    : IRequestHandler<GetRevenueSummaryQuery, RevenueSummaryDto>
{
    public Task<RevenueSummaryDto> Handle(GetRevenueSummaryQuery request, CancellationToken ct)
        => revenueQueryService.GetSummaryAsync(request.From, request.To, ct);
}
