using DentalClinic.API.Application.DTOs.Revenue;

namespace DentalClinic.API.Application.DTOs.Finance;

public record FinanceOverviewDto(
    decimal TotalRevenue,
    decimal TotalExpense,
    decimal TotalPayroll,
    decimal Profit,
    double RevenueGrowthPercent,
    double ExpenseGrowthPercent,
    double ProfitGrowthPercent,
    IReadOnlyList<RevenueByServiceDto> TopServices,
    IReadOnlyList<RevenueByDentistDto> TopDentists,
    IReadOnlyList<RevenueTransactionDto> RecentTransactions);
