using DentalClinic.API.Application.DTOs.Expenses;
using DentalClinic.API.Domain.Entities;

namespace DentalClinic.API.Application.UseCases.Expenses;

internal static class ExpenseMapper
{
    public static ExpenseDto ToDto(Expense e) => new(
        e.Id,
        e.Category.ToString(),
        e.Description,
        e.Amount,
        e.Date,
        e.Note,
        e.IsRecurring,
        e.Frequency?.ToString(),
        e.RecurringSourceId,
        e.CreatedAt);
}
