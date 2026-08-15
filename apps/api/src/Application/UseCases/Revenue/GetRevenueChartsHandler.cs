using DentalClinic.API.Application.DTOs.Revenue;
using DentalClinic.API.Application.Interfaces;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Revenue;

public record GetRevenueChartsQuery(DateOnly From, DateOnly To) : IRequest<RevenueChartsDto>;

public class GetRevenueChartsHandler(IRevenueQueryService revenueQueryService)
    : IRequestHandler<GetRevenueChartsQuery, RevenueChartsDto>
{
    public Task<RevenueChartsDto> Handle(GetRevenueChartsQuery request, CancellationToken ct)
        => revenueQueryService.GetChartsAsync(request.From, request.To, ct);
}
