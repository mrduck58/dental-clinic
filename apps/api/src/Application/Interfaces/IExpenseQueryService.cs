using DentalClinic.API.Application.DTOs.Expenses;

namespace DentalClinic.API.Application.Interfaces;

public interface IExpenseQueryService
{
    Task<ExpenseSummaryDto> GetSummaryAsync(DateOnly from, DateOnly to, CancellationToken ct);

    Task<ExpenseChartsDto> GetChartsAsync(DateOnly from, DateOnly to, CancellationToken ct);
}
