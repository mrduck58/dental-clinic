using DentalClinic.API.Application.DTOs.Finance;
using DentalClinic.API.Application.Interfaces;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Finance;

public record GetFinanceOverviewQuery(DateOnly From, DateOnly To) : IRequest<FinanceOverviewDto>;

public class GetFinanceOverviewHandler(IFinanceOverviewQueryService financeOverviewQueryService)
    : IRequestHandler<GetFinanceOverviewQuery, FinanceOverviewDto>
{
    public Task<FinanceOverviewDto> Handle(GetFinanceOverviewQuery request, CancellationToken ct)
        => financeOverviewQueryService.GetOverviewAsync(request.From, request.To, ct);
}
