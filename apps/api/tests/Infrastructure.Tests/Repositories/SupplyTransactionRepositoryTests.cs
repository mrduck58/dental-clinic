using DentalClinic.API.Domain.Common;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Infrastructure.Persistence;
using DentalClinic.API.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Repositories;

[TestFixture]
public class SupplyTransactionRepositoryTests
{
    private AppDbContext _db = null!;
    private SupplyTransactionRepository _sut = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _sut = new SupplyTransactionRepository(_db);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    /// <summary>Drill-down phải khớp CHÍNH XÁC tập bản ghi mà ExpenseQueryService.TotalSupply cộng —
    /// dùng cùng VietnamPeriod.Bounds nên biên ngày phải trùng nhau tuyệt đối.</summary>
    [Test]
    public async Task GetImportsInRangeAsync_MatchesVietnamPeriodBounds()
    {
        var item = SupplyItem.Create("SP-01", "Găng tay", "Vật tư tiêu hao", "hộp", 10, 2);
        _db.SupplyItems.Add(item);

        var inRange = SupplyTransaction.Create(item.Id, "import", 5, null, "staff1", unitPrice: 100_000m);
        var beforeRange = SupplyTransaction.Create(item.Id, "import", 3, null, "staff1", unitPrice: 100_000m);
        var afterRange = SupplyTransaction.Create(item.Id, "import", 2, null, "staff1", unitPrice: 100_000m);
        _db.SupplyTransactions.AddRange(inRange, beforeRange, afterRange);
        await _db.SaveChangesAsync();

        SetCreatedAt(inRange, new DateTimeOffset(2026, 8, 10, 3, 0, 0, TimeSpan.Zero));
        SetCreatedAt(beforeRange, new DateTimeOffset(2026, 7, 31, 16, 59, 0, TimeSpan.Zero)); // 23:59 giờ VN ngày 31/7
        SetCreatedAt(afterRange, new DateTimeOffset(2026, 8, 31, 17, 0, 0, TimeSpan.Zero)); // 00:00 giờ VN ngày 1/9
        await _db.SaveChangesAsync();

        var (start, end) = VietnamPeriod.Bounds(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31));
        var result = (await _sut.GetImportsInRangeAsync(start, end)).ToList();

        result.Should().ContainSingle().Which.Id.Should().Be(inRange.Id);
    }

    /// <summary>Giao dịch xuất kho (Type="export") không được tính là nhập, dù cùng khoảng ngày.</summary>
    [Test]
    public async Task GetImportsInRangeAsync_ExcludesExportTransactions()
    {
        var item = SupplyItem.Create("SP-02", "Khẩu trang", "Vật tư tiêu hao", "hộp", 10, 2);
        _db.SupplyItems.Add(item);
        var export = SupplyTransaction.Create(item.Id, "export", 1, null, "staff1");
        _db.SupplyTransactions.Add(export);
        await _db.SaveChangesAsync();

        var (start, end) = VietnamPeriod.Bounds(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));
        var result = await _sut.GetImportsInRangeAsync(start, end);

        result.Should().BeEmpty();
    }

    /// <summary>Nhập kho chưa có đơn giá (dữ liệu cũ/thiếu) không được tính vào drill-down, khớp đúng
    /// điều kiện UnitPrice != null mà ExpenseQueryService đã dùng.</summary>
    [Test]
    public async Task GetImportsInRangeAsync_ExcludesImportsWithoutUnitPrice()
    {
        var item = SupplyItem.Create("SP-03", "Bông gạc", "Vật tư tiêu hao", "gói", 10, 2);
        _db.SupplyItems.Add(item);
        var noPriceImport = SupplyTransaction.Create(item.Id, "import", 4, null, "staff1");
        _db.SupplyTransactions.Add(noPriceImport);
        await _db.SaveChangesAsync();

        var (start, end) = VietnamPeriod.Bounds(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));
        var result = await _sut.GetImportsInRangeAsync(start, end);

        result.Should().BeEmpty();
    }

    private static void SetCreatedAt(SupplyTransaction txn, DateTimeOffset date)
        => typeof(SupplyTransaction).GetProperty(nameof(SupplyTransaction.CreatedAt))!.SetValue(txn, date);
}
