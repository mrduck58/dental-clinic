using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Infrastructure.Persistence.Repositories;

public class ExpenseRepository(AppDbContext db) : IExpenseRepository
{
    public async Task<(IReadOnlyList<Expense> Items, int TotalCount)> GetPagedAsync(
        DateOnly from,
        DateOnly to,
        ExpenseCategory? category,
        string? search,
        int page,
        int pageSize,
        string? sortBy,
        string? sortDir,
        CancellationToken ct = default)
    {
        var query = db.Expenses.AsNoTracking().Where(e => e.Date >= from && e.Date <= to);

        if (category is ExpenseCategory c)
            query = query.Where(e => e.Category == c);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(e => e.Description.Contains(term) || (e.Note != null && e.Note.Contains(term)));
        }

        var totalCount = await query.CountAsync(ct);

        var dir = string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase) ? "asc" : "desc";
        query = sortBy switch
        {
            "amount" => dir == "asc" ? query.OrderBy(e => e.Amount) : query.OrderByDescending(e => e.Amount),
            "category" => dir == "asc" ? query.OrderBy(e => e.Category) : query.OrderByDescending(e => e.Category),
            _ => dir == "asc" ? query.OrderBy(e => e.Date) : query.OrderByDescending(e => e.Date),
        };

        var safePage = page < 1 ? 1 : page;
        var safePageSize = pageSize < 1 ? 20 : pageSize;

        var items = await query
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<Expense?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Expenses.FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<IReadOnlyList<Expense>> GetActiveRecurringTemplatesAsync(CancellationToken ct = default)
        => await db.Expenses.Where(e => e.IsRecurring).ToListAsync(ct);

    public async Task<bool> HasRecurrenceInstanceInPeriodAsync(
        Guid sourceId, DateOnly periodStart, DateOnly periodEnd, CancellationToken ct = default)
        => await db.Expenses.AnyAsync(
            e => e.RecurringSourceId == sourceId && e.Date >= periodStart && e.Date <= periodEnd, ct);

    public async Task<IReadOnlyList<Expense>> GetInRangeAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
        => await db.Expenses.AsNoTracking().Where(e => e.Date >= from && e.Date <= to).ToListAsync(ct);

    public async Task AddAsync(Expense expense, CancellationToken ct = default)
        => await db.Expenses.AddAsync(expense, ct);

    public async Task AddRangeAsync(IEnumerable<Expense> expenses, CancellationToken ct = default)
        => await db.Expenses.AddRangeAsync(expenses, ct);

    public Task DeleteAsync(Expense expense, CancellationToken ct = default)
    {
        db.Expenses.Remove(expense);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await db.SaveChangesAsync(ct);
}
