using DentalClinic.API.Application.DTOs.Expenses;
using DentalClinic.API.Application.Interfaces;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Expenses;

public record GetExpenseChartsQuery(DateOnly From, DateOnly To) : IRequest<ExpenseChartsDto>;

public class GetExpenseChartsHandler(IExpenseQueryService expenseQueryService)
    : IRequestHandler<GetExpenseChartsQuery, ExpenseChartsDto>
{
    public Task<ExpenseChartsDto> Handle(GetExpenseChartsQuery request, CancellationToken ct)
        => expenseQueryService.GetChartsAsync(request.From, request.To, ct);
}
