using DentalClinic.API.Application.DTOs.Expenses;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Expenses;

public record GetExpensesPagedQuery(
    DateOnly From,
    DateOnly To,
    string? Category,
    string? Search,
    int Page,
    int PageSize,
    string? SortBy,
    string? SortDir) : IRequest<ExpensesPagedDto>;

public class GetExpensesPagedHandler(IExpenseRepository expenseRepository)
    : IRequestHandler<GetExpensesPagedQuery, ExpensesPagedDto>
{
    public async Task<ExpensesPagedDto> Handle(GetExpensesPagedQuery query, CancellationToken ct)
    {
        ExpenseCategory? category = null;
        if (!string.IsNullOrWhiteSpace(query.Category) && Enum.TryParse<ExpenseCategory>(query.Category, true, out var c))
            category = c;

        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 20 : query.PageSize;

        var (items, totalCount) = await expenseRepository.GetPagedAsync(
            query.From, query.To, category, query.Search, page, pageSize, query.SortBy, query.SortDir, ct);

        return new ExpensesPagedDto(
            items.Select(ExpenseMapper.ToDto).ToList(),
            totalCount,
            page,
            pageSize,
            Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize)));
    }
}
