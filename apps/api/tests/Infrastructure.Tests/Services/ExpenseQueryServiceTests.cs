using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Infrastructure.Persistence;
using DentalClinic.API.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Services;

[TestFixture]
public class ExpenseQueryServiceTests
{
    private AppDbContext _db = null!;
    private ExpenseQueryService _sut = null!;

    private static readonly DateOnly PeriodFrom = new(2026, 8, 1);
    private static readonly DateOnly PeriodTo = new(2026, 8, 31);

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _sut = new ExpenseQueryService(_db);
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    [Test]
    public async Task GetSummaryAsync_ExpenseInPeriod_CountsTowardTotalOther()
    {
        var expense = Expense.Create(ExpenseCategory.Rent, "Thuê mặt bằng", 5_000_000m, new DateOnly(2026, 8, 5), null, false, null);
        _db.Expenses.Add(expense);
        await _db.SaveChangesAsync();

        var result = await _sut.GetSummaryAsync(PeriodFrom, PeriodTo, CancellationToken.None);

        result.TotalOther.Should().Be(5_000_000m);
        result.TotalExpense.Should().Be(5_000_000m);
    }

    [Test]
    public async Task GetSummaryAsync_SupplyImportInPeriod_CountsTowardTotalSupply()
    {
        var item = SupplyItem.Create("SP-01", "Găng tay", "Vật tư tiêu hao", "hộp", 10, 2);
        _db.SupplyItems.Add(item);
        var txn = SupplyTransaction.Create(item.Id, "import", 5, null, "staff1", unitPrice: 100_000m);
        _db.SupplyTransactions.Add(txn);
        await _db.SaveChangesAsync();
        SetCreatedAt(txn, new DateTimeOffset(2026, 8, 10, 3, 0, 0, TimeSpan.Zero));
        await _db.SaveChangesAsync();

        var result = await _sut.GetSummaryAsync(PeriodFrom, PeriodTo, CancellationToken.None);

        result.TotalSupply.Should().Be(500_000m); // 5 x 100_000
        result.TotalExpense.Should().Be(500_000m);
    }

    [Test]
    public async Task GetSummaryAsync_PayrollInPeriod_CountsTowardTotalPayroll()
    {
        var user = User.Create("dt1", $"dt1-{Guid.NewGuid()}@test.com", "hash", UserRole.Dentist);
        _db.Users.Add(user);
        var record = PayrollRecord.CreateDraft(user.Id, 2026, 8, 10_000_000m, 0m, 0, 0, 0m, 0m, 0m);
        _db.PayrollRecords.Add(record);
        await _db.SaveChangesAsync();

        var result = await _sut.GetSummaryAsync(PeriodFrom, PeriodTo, CancellationToken.None);

        result.TotalPayroll.Should().Be(10_000_000m);
        result.TotalExpense.Should().Be(10_000_000m);
    }

    [Test]
    public async Task GetSummaryAsync_AllThreeSources_CombinesIntoTotalExpense()
    {
        var expense = Expense.Create(ExpenseCategory.Marketing, "Quảng cáo Facebook", 2_000_000m, new DateOnly(2026, 8, 3), null, false, null);
        _db.Expenses.Add(expense);

        var item = SupplyItem.Create("SP-02", "Bông gạc", "Vật tư tiêu hao", "gói", 20, 5);
        _db.SupplyItems.Add(item);
        var txn = SupplyTransaction.Create(item.Id, "import", 3, null, "staff2", unitPrice: 50_000m);
        _db.SupplyTransactions.Add(txn);

        var user = User.Create("dt2", $"dt2-{Guid.NewGuid()}@test.com", "hash", UserRole.Dentist);
        _db.Users.Add(user);
        var payroll = PayrollRecord.CreateDraft(user.Id, 2026, 8, 8_000_000m, 0m, 0, 0, 0m, 0m, 0m);
        _db.PayrollRecords.Add(payroll);

        await _db.SaveChangesAsync();
        SetCreatedAt(txn, new DateTimeOffset(2026, 8, 12, 3, 0, 0, TimeSpan.Zero));
        await _db.SaveChangesAsync();

        var result = await _sut.GetSummaryAsync(PeriodFrom, PeriodTo, CancellationToken.None);

        // 2tr (expense) + 150k (3 x 50k vật tư) + 8tr (lương) = 10.15 triệu
        result.TotalExpense.Should().Be(10_150_000m);
    }

    [Test]
    public async Task GetChartsAsync_IncludesSupplyAndPayrollAsAdditionalCategories()
    {
        var expense = Expense.Create(ExpenseCategory.Software, "Phần mềm CRM", 1_000_000m, new DateOnly(2026, 8, 4), null, false, null);
        _db.Expenses.Add(expense);

        var item = SupplyItem.Create("SP-03", "Kim tiêm", "Vật tư tiêu hao", "hộp", 10, 2);
        _db.SupplyItems.Add(item);
        var txn = SupplyTransaction.Create(item.Id, "import", 2, null, "staff3", unitPrice: 200_000m);
        _db.SupplyTransactions.Add(txn);

        var user = User.Create("dt3", $"dt3-{Guid.NewGuid()}@test.com", "hash", UserRole.Dentist);
        _db.Users.Add(user);
        var payroll = PayrollRecord.CreateDraft(user.Id, 2026, 8, 6_000_000m, 0m, 0, 0, 0m, 0m, 0m);
        _db.PayrollRecords.Add(payroll);

        await _db.SaveChangesAsync();
        SetCreatedAt(txn, new DateTimeOffset(2026, 8, 12, 3, 0, 0, TimeSpan.Zero));
        await _db.SaveChangesAsync();

        var result = await _sut.GetChartsAsync(PeriodFrom, PeriodTo, CancellationToken.None);

        result.ByCategory.Should().Contain(c => c.CategoryLabel == "Phần mềm" && c.Amount == 1_000_000m);
        result.ByCategory.Should().Contain(c => c.CategoryLabel == "Vật tư" && c.Amount == 400_000m);
        result.ByCategory.Should().Contain(c => c.CategoryLabel == "Lương" && c.Amount == 6_000_000m);
    }

    private static void SetCreatedAt(SupplyTransaction txn, DateTimeOffset date)
        => typeof(SupplyTransaction).GetProperty(nameof(SupplyTransaction.CreatedAt))!.SetValue(txn, date);
}
