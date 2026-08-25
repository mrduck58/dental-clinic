using DentalClinic.API.Application.DTOs.Expenses;
using DentalClinic.API.Application.Interfaces;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Infrastructure.Services;

/// <summary>
/// Tổng hợp chi phí trên cả 3 nguồn: Expense (nhập tay), SupplyTransaction (vật tư), PayrollRecord (lương) —
/// TotalOther/TotalSupply/TotalPayroll tách riêng để trang Tổng quan tài chính hiển thị Lương như một KPI
/// độc lập thay vì gộp chung vào "Chi phí".
/// </summary>
public class ExpenseQueryService(AppDbContext db) : IExpenseQueryService
{
    private static readonly TimeZoneInfo VietnamTz = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

    public async Task<ExpenseSummaryDto> GetSummaryAsync(DateOnly from, DateOnly to, CancellationToken ct)
    {
        var totalOther = await db.Expenses
            .AsNoTracking()
            .Where(e => e.Date >= from && e.Date <= to)
            .SumAsync(e => e.Amount, ct);

        var (start, end) = PeriodBounds(from, to);

        var totalSupply = await db.SupplyTransactions
            .AsNoTracking()
            .Where(t => t.Type == "import" && t.UnitPrice != null && t.CreatedAt >= start && t.CreatedAt < end)
            .SumAsync(t => (t.UnitPrice ?? 0m) * t.Quantity, ct);

        var payrolls = await db.PayrollRecords
            .AsNoTracking()
            .Select(p => new { p.Year, p.Month, p.NetSalary })
            .ToListAsync(ct);
        var totalPayroll = payrolls
            .Where(p => PeriodDate(p.Year, p.Month) >= start && PeriodDate(p.Year, p.Month) < end)
            .Sum(p => p.NetSalary);

        return new ExpenseSummaryDto(totalOther + totalSupply + totalPayroll, totalOther, totalSupply, totalPayroll);
    }

    public async Task<ExpenseChartsDto> GetChartsAsync(DateOnly from, DateOnly to, CancellationToken ct)
    {
        var summary = await GetSummaryAsync(from, to, ct);

        var otherRows = await db.Expenses
            .AsNoTracking()
            .Where(e => e.Date >= from && e.Date <= to)
            .Select(e => new { e.Category, e.Amount })
            .ToListAsync(ct);

        var categories = otherRows
            .GroupBy(e => e.Category)
            .Select(g => new ExpenseByCategoryDto(CategoryLabel(g.Key), g.Sum(e => e.Amount)))
            .ToList();

        if (summary.TotalSupply > 0) categories.Add(new ExpenseByCategoryDto("Vật tư", summary.TotalSupply));
        if (summary.TotalPayroll > 0) categories.Add(new ExpenseByCategoryDto("Lương", summary.TotalPayroll));

        return new ExpenseChartsDto(categories.OrderByDescending(c => c.Amount).ToList());
    }

    private (DateTimeOffset Start, DateTimeOffset End) PeriodBounds(DateOnly from, DateOnly to)
    {
        var (fromDate, toDate) = from > to ? (to, from) : (from, to);
        return (ToVn(fromDate), ToVn(toDate.AddDays(1)));
    }

    private DateTimeOffset ToVn(DateOnly date)
        => new DateTimeOffset(date.Year, date.Month, date.Day, 0, 0, 0, VietnamTz.BaseUtcOffset).ToUniversalTime();

    private static DateTimeOffset PeriodDate(int year, int month)
        => new(new DateTime(year, month, 1), TimeSpan.FromHours(7));

    private static string CategoryLabel(ExpenseCategory category) => category switch
    {
        ExpenseCategory.Medicine => "Thuốc",
        ExpenseCategory.Equipment => "Thiết bị",
        ExpenseCategory.Rent => "Thuê mặt bằng",
        ExpenseCategory.Utilities => "Điện nước",
        ExpenseCategory.Marketing => "Marketing",
        ExpenseCategory.Maintenance => "Bảo trì",
        ExpenseCategory.Software => "Phần mềm",
        ExpenseCategory.Other => "Khác",
        _ => category.ToString(),
    };
}
