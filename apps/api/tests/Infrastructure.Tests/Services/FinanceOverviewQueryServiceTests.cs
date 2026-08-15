using DentalClinic.API.Application.DTOs.Expenses;
using DentalClinic.API.Application.DTOs.Revenue;
using DentalClinic.API.Application.Interfaces;
using DentalClinic.API.Infrastructure.Services;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Services;

[TestFixture]
public class FinanceOverviewQueryServiceTests
{
    private IRevenueQueryService _revenue = null!;
    private IExpenseQueryService _expense = null!;
    private FinanceOverviewQueryService _sut = null!;

    private static readonly DateOnly From = new(2026, 8, 1);
    private static readonly DateOnly To = new(2026, 8, 31);
    // Kỳ liền trước cùng độ dài (31 ngày) là 2026-07-01 → 2026-07-31.
    private static readonly DateOnly PrevFrom = new(2026, 7, 1);
    private static readonly DateOnly PrevTo = new(2026, 7, 31);

    [SetUp]
    public void SetUp()
    {
        _revenue = Substitute.For<IRevenueQueryService>();
        _expense = Substitute.For<IExpenseQueryService>();
        _sut = new FinanceOverviewQueryService(_revenue, _expense);

        _revenue.GetChartsAsync(From, To, Arg.Any<CancellationToken>())
            .Returns(new RevenueChartsDto([], []));
        _revenue.GetTransactionsPagedAsync(Arg.Any<RevenueTransactionsFilter>(), Arg.Any<CancellationToken>())
            .Returns(new RevenueTransactionsPagedDto([], 0, 1, 5, 1));
    }

    [Test]
    public async Task GetOverviewAsync_ExpenseExcludesPayroll_ProfitComputedCorrectly()
    {
        _revenue.GetSummaryAsync(From, To, Arg.Any<CancellationToken>())
            .Returns(new RevenueSummaryDto(20_000_000m, 15_000_000m, 5_000_000m, 0m));
        _expense.GetSummaryAsync(From, To, Arg.Any<CancellationToken>())
            .Returns(new ExpenseSummaryDto(TotalExpense: 8_000_000m, TotalOther: 2_000_000m, TotalSupply: 1_000_000m, TotalPayroll: 5_000_000m));

        _revenue.GetSummaryAsync(PrevFrom, PrevTo, Arg.Any<CancellationToken>())
            .Returns(new RevenueSummaryDto(0m, 0m, 0m, 0m));
        _expense.GetSummaryAsync(PrevFrom, PrevTo, Arg.Any<CancellationToken>())
            .Returns(new ExpenseSummaryDto(0m, 0m, 0m, 0m));

        var result = await _sut.GetOverviewAsync(From, To, CancellationToken.None);

        result.TotalRevenue.Should().Be(15_000_000m); // TotalCollected
        result.TotalExpense.Should().Be(3_000_000m);   // TotalOther + TotalSupply, KHÔNG gồm lương
        result.TotalPayroll.Should().Be(5_000_000m);
        result.Profit.Should().Be(15_000_000m - 3_000_000m - 5_000_000m); // = 7tr
    }

    [Test]
    public async Task GetOverviewAsync_ComparesAgainstPreviousPeriodOfSameLength()
    {
        _revenue.GetSummaryAsync(From, To, Arg.Any<CancellationToken>())
            .Returns(new RevenueSummaryDto(0m, 10_000_000m, 0m, 0m));
        _expense.GetSummaryAsync(From, To, Arg.Any<CancellationToken>())
            .Returns(new ExpenseSummaryDto(0m, 0m, 0m, 0m));

        _revenue.GetSummaryAsync(PrevFrom, PrevTo, Arg.Any<CancellationToken>())
            .Returns(new RevenueSummaryDto(0m, 5_000_000m, 0m, 0m));
        _expense.GetSummaryAsync(PrevFrom, PrevTo, Arg.Any<CancellationToken>())
            .Returns(new ExpenseSummaryDto(0m, 0m, 0m, 0m));

        var result = await _sut.GetOverviewAsync(From, To, CancellationToken.None);

        // (10tr - 5tr) / 5tr * 100 = 100%
        result.RevenueGrowthPercent.Should().Be(100);
        await _revenue.Received(1).GetSummaryAsync(PrevFrom, PrevTo, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetOverviewAsync_PreviousPeriodZero_CurrentZero_GrowthIsZeroNotHundred()
    {
        _revenue.GetSummaryAsync(From, To, Arg.Any<CancellationToken>())
            .Returns(new RevenueSummaryDto(0m, 0m, 0m, 0m));
        _expense.GetSummaryAsync(From, To, Arg.Any<CancellationToken>())
            .Returns(new ExpenseSummaryDto(0m, 0m, 0m, 0m));
        _revenue.GetSummaryAsync(PrevFrom, PrevTo, Arg.Any<CancellationToken>())
            .Returns(new RevenueSummaryDto(0m, 0m, 0m, 0m));
        _expense.GetSummaryAsync(PrevFrom, PrevTo, Arg.Any<CancellationToken>())
            .Returns(new ExpenseSummaryDto(0m, 0m, 0m, 0m));

        var result = await _sut.GetOverviewAsync(From, To, CancellationToken.None);

        result.RevenueGrowthPercent.Should().Be(0);
        result.ProfitGrowthPercent.Should().Be(0);
    }
}
