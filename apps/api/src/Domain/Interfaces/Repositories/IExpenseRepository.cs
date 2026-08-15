using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;

namespace DentalClinic.API.Domain.Interfaces.Repositories;

public interface IExpenseRepository
{
    Task<(IReadOnlyList<Expense> Items, int TotalCount)> GetPagedAsync(
        DateOnly from,
        DateOnly to,
        ExpenseCategory? category,
        string? search,
        int page,
        int pageSize,
        string? sortBy,
        string? sortDir,
        CancellationToken ct = default);

    Task<Expense?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Các bản ghi mẫu định kỳ đang hoạt động, dùng để sinh bản ghi cho kỳ mới.</summary>
    Task<IReadOnlyList<Expense>> GetActiveRecurringTemplatesAsync(CancellationToken ct = default);

    /// <summary>Đã có bản ghi được sinh từ mẫu này rơi vào khoảng [periodStart, periodEnd] chưa.</summary>
    Task<bool> HasRecurrenceInstanceInPeriodAsync(Guid sourceId, DateOnly periodStart, DateOnly periodEnd, CancellationToken ct = default);

    /// <summary>Toàn bộ chi phí (Expense) trong khoảng ngày — dùng cho tổng hợp/biểu đồ theo danh mục.</summary>
    Task<IReadOnlyList<Expense>> GetInRangeAsync(DateOnly from, DateOnly to, CancellationToken ct = default);

    Task AddAsync(Expense expense, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<Expense> expenses, CancellationToken ct = default);
    Task DeleteAsync(Expense expense, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
