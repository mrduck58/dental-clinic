using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Infrastructure.Persistence;
using DentalClinic.API.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Repositories;

[TestFixture]
public class ActivityLogRepositoryTests
{
    private AppDbContext _db = null!;
    private ActivityLogRepository _sut = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _sut = new ActivityLogRepository(_db);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    /// <summary>AddAsync phải lưu bản ghi vào DB.</summary>
    [Test]
    public async Task AddAsync_ValidLog_PersistsToDatabase()
    {
        var log = MakeLog(action: "Create", module: "Account");

        await _sut.AddAsync(log);

        (await _db.ActivityLogs.CountAsync()).Should().Be(1);
    }

    /// <summary>GetPagedAsync không có filter phải trả về tất cả, sắp xếp mới nhất trước.</summary>
    [Test]
    public async Task GetPagedAsync_NoFilter_ReturnsAllOrderedByCreatedAtDescending()
    {
        var older = MakeLog(createdAt: DateTimeOffset.UtcNow.AddHours(-2));
        var newer = MakeLog(createdAt: DateTimeOffset.UtcNow);
        _db.ActivityLogs.AddRange(older, newer);
        await _db.SaveChangesAsync();

        var (items, total) = await _sut.GetPagedAsync(null, null, null, null, null, null, null, page: 1, pageSize: 10);

        total.Should().Be(2);
        items[0].Id.Should().Be(newer.Id);
        items[1].Id.Should().Be(older.Id);
    }

    /// <summary>Lọc theo userId phải chỉ trả về log của đúng user đó.</summary>
    [Test]
    public async Task GetPagedAsync_FilterByUserId_ReturnsOnlyMatchingUser()
    {
        var userId = Guid.NewGuid();
        _db.ActivityLogs.AddRange(MakeLog(userId: userId), MakeLog(userId: Guid.NewGuid()));
        await _db.SaveChangesAsync();

        var (items, total) = await _sut.GetPagedAsync(userId, null, null, null, null, null, null, page: 1, pageSize: 10);

        total.Should().Be(1);
        items[0].UserId.Should().Be(userId);
    }

    /// <summary>Lọc theo action/module/status phải áp dụng đồng thời (AND).</summary>
    [Test]
    public async Task GetPagedAsync_FilterByActionModuleStatus_AppliesAllFilters()
    {
        _db.ActivityLogs.AddRange(
            MakeLog(action: "Create", module: "Account", status: "Success"),
            MakeLog(action: "Create", module: "Account", status: "Failed"),
            MakeLog(action: "Edit", module: "Account", status: "Success"));
        await _db.SaveChangesAsync();

        var (items, total) = await _sut.GetPagedAsync(null, "Create", "Account", "Success", null, null, null, page: 1, pageSize: 10);

        total.Should().Be(1);
        items[0].Status.Should().Be("Success");
        items[0].Action.Should().Be("Create");
    }

    /// <summary>Lọc theo khoảng ngày phải loại bỏ log ngoài khoảng.</summary>
    [Test]
    public async Task GetPagedAsync_FilterByDateRange_ExcludesOutOfRangeLogs()
    {
        var inRange = MakeLog(createdAt: new DateTimeOffset(2026, 6, 15, 0, 0, 0, TimeSpan.Zero));
        var beforeRange = MakeLog(createdAt: new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero));
        var afterRange = MakeLog(createdAt: new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero));
        _db.ActivityLogs.AddRange(inRange, beforeRange, afterRange);
        await _db.SaveChangesAsync();

        var (items, total) = await _sut.GetPagedAsync(
            null, null, null, null, null,
            startDate: new DateTimeOffset(2026, 6, 10, 0, 0, 0, TimeSpan.Zero),
            endDate: new DateTimeOffset(2026, 6, 20, 0, 0, 0, TimeSpan.Zero),
            page: 1, pageSize: 10);

        total.Should().Be(1);
        items[0].Id.Should().Be(inRange.Id);
    }

    /// <summary>excludeAction phải loại bỏ đúng loại thao tác đó, giữ nguyên các loại khác — dùng để
    /// Audit Log không hiển thị log đăng nhập nữa (đã có màn hình Lịch sử đăng nhập riêng).</summary>
    [Test]
    public async Task GetPagedAsync_ExcludeAction_RemovesOnlyThatAction()
    {
        _db.ActivityLogs.AddRange(
            MakeLog(action: "login"),
            MakeLog(action: "Create"));
        await _db.SaveChangesAsync();

        var (items, total) = await _sut.GetPagedAsync(
            null, null, null, null, null, null, null, page: 1, pageSize: 10, excludeAction: "login");

        total.Should().Be(1);
        items[0].Action.Should().Be("Create");
    }

    /// <summary>Phân trang phải cắt đúng số lượng theo page/pageSize, tổng đếm không đổi.</summary>
    [Test]
    public async Task GetPagedAsync_Pagination_ReturnsCorrectSlice()
    {
        for (var i = 0; i < 5; i++)
            _db.ActivityLogs.Add(MakeLog(createdAt: DateTimeOffset.UtcNow.AddMinutes(-i)));
        await _db.SaveChangesAsync();

        var (items, total) = await _sut.GetPagedAsync(null, null, null, null, null, null, null, page: 2, pageSize: 2);

        total.Should().Be(5);
        items.Should().HaveCount(2);
    }

    private static ActivityLog MakeLog(
        Guid? userId = null,
        string action = "Create",
        string module = "Account",
        string status = "Success",
        DateTimeOffset? createdAt = null)
    {
        var log = ActivityLog.Create(
            userId ?? Guid.NewGuid(), "user1", "Admin", action, module, "Mô tả test", status);
        if (createdAt.HasValue)
            typeof(ActivityLog).GetProperty(nameof(ActivityLog.CreatedAt))!.SetValue(log, createdAt.Value);
        return log;
    }
}
