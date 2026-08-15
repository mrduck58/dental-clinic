using DentalClinic.API.Application.DTOs.Expenses;
using DentalClinic.API.Application.Interfaces;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Expenses;

public record GetExpenseSummaryQuery(DateOnly From, DateOnly To) : IRequest<ExpenseSummaryDto>;

public class GetExpenseSummaryHandler(IExpenseQueryService expenseQueryService)
    : IRequestHandler<GetExpenseSummaryQuery, ExpenseSummaryDto>
{
    public Task<ExpenseSummaryDto> Handle(GetExpenseSummaryQuery request, CancellationToken ct)
        => expenseQueryService.GetSummaryAsync(request.From, request.To, ct);
}
