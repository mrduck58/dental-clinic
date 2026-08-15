namespace DentalClinic.API.Application.DTOs.Expenses;

public record ExpenseDto(
    Guid Id,
    string Category,
    string Description,
    decimal Amount,
    DateOnly Date,
    string? Note,
    bool IsRecurring,
    string? Frequency,
    Guid? RecurringSourceId,
    DateTimeOffset CreatedAt);

public record ExpensesPagedDto(
    IReadOnlyList<ExpenseDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);

public record CreateExpenseRequest(
    string Category,
    string Description,
    decimal Amount,
    DateOnly Date,
    string? Note,
    bool IsRecurring,
    string? Frequency);

public record UpdateExpenseRequest(
    string Category,
    string Description,
    decimal Amount,
    DateOnly Date,
    string? Note,
    bool IsRecurring,
    string? Frequency);

/// <summary>
/// Tổng chi phí trong kỳ — TotalExpense (dùng cho trang Chi phí) = TotalOther + TotalSupply + TotalPayroll.
/// TotalOther tách riêng để trang Tổng quan tài chính có thể hiển thị "Chi phí" và "Lương" là 2 KPI riêng
/// (không cộng trùng lương vào cả hai).
/// </summary>
public record ExpenseSummaryDto(
    decimal TotalExpense,
    decimal TotalOther,
    decimal TotalSupply,
    decimal TotalPayroll);

public record ExpenseByCategoryDto(string CategoryLabel, decimal Amount);

public record ExpenseChartsDto(IReadOnlyList<ExpenseByCategoryDto> ByCategory);

public record GenerateRecurringExpensesResult(int GeneratedCount);
