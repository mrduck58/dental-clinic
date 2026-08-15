using DentalClinic.API.Application.DTOs.Finance;
using DentalClinic.API.Application.Interfaces;

namespace DentalClinic.API.Infrastructure.Services;

/// <summary>
/// Tổng hợp Doanh thu (đã thu) + Chi phí (không gồm lương) + Lương thành 3 KPI riêng biệt của trang
/// Tổng quan tài chính, cộng % so với kỳ liền trước cùng độ dài. Tái dùng RevenueQueryService/
/// ExpenseQueryService đã có thay vì query lại từ đầu.
/// </summary>
public class FinanceOverviewQueryService(
    IRevenueQueryService revenueQueryService,
    IExpenseQueryService expenseQueryService) : IFinanceOverviewQueryService
{
    public async Task<FinanceOverviewDto> GetOverviewAsync(DateOnly from, DateOnly to, CancellationToken ct)
    {
        var (periodFrom, periodTo) = from > to ? (to, from) : (from, to);

        var revenueSummary = await revenueQueryService.GetSummaryAsync(periodFrom, periodTo, ct);
        var expenseSummary = await expenseQueryService.GetSummaryAsync(periodFrom, periodTo, ct);
        var charts = await revenueQueryService.GetChartsAsync(periodFrom, periodTo, ct);
        var transactions = await revenueQueryService.GetTransactionsPagedAsync(
            new RevenueTransactionsFilter(periodFrom, periodTo, null, null, null, null, null, 1, 5, "date", "desc"), ct);

        var totalRevenue = revenueSummary.TotalCollected;
        var totalExpense = expenseSummary.TotalOther + expenseSummary.TotalSupply;
        var totalPayroll = expenseSummary.TotalPayroll;
        var profit = totalRevenue - totalExpense - totalPayroll;

        var days = periodTo.DayNumber - periodFrom.DayNumber + 1;
        var prevTo = periodFrom.AddDays(-1);
        var prevFrom = prevTo.AddDays(-(days - 1));

        var prevRevenue = await revenueQueryService.GetSummaryAsync(prevFrom, prevTo, ct);
        var prevExpense = await expenseQueryService.GetSummaryAsync(prevFrom, prevTo, ct);
        var prevTotalExpense = prevExpense.TotalOther + prevExpense.TotalSupply;
        var prevProfit = prevRevenue.TotalCollected - prevTotalExpense - prevExpense.TotalPayroll;

        return new FinanceOverviewDto(
            totalRevenue,
            totalExpense,
            totalPayroll,
            profit,
            GrowthPercent(totalRevenue, prevRevenue.TotalCollected),
            GrowthPercent(totalExpense, prevTotalExpense),
            GrowthPercent(profit, prevProfit),
            charts.ByService.Take(5).ToList(),
            charts.ByDentist.Take(5).ToList(),
            transactions.Items);
    }

    private static double GrowthPercent(decimal current, decimal previous)
    {
        if (previous == 0) return current == 0 ? 0 : 100;
        return (double)Math.Round((current - previous) / previous * 100, 1);
    }
}
