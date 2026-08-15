using DentalClinic.API.Application.DTOs.Commissions;
using DentalClinic.API.Application.Interfaces;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Commissions;

public record GetCommissionRulesQuery(DateOnly From, DateOnly To) : IRequest<CommissionRulesResultDto>;

public class GetCommissionRulesHandler(ICommissionQueryService commissionQueryService)
    : IRequestHandler<GetCommissionRulesQuery, CommissionRulesResultDto>
{
    public Task<CommissionRulesResultDto> Handle(GetCommissionRulesQuery request, CancellationToken ct)
        => commissionQueryService.GetRulesWithCommissionAsync(request.From, request.To, ct);
}
