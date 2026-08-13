using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Infrastructure.Persistence;
using DentalClinic.API.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Repositories;

[TestFixture]
public class LeaveRequestRepositoryTests
{
    private AppDbContext _db = null!;
    private LeaveRequestRepository _sut = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _sut = new LeaveRequestRepository(_db);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    // ── GetAllAsync ───────────────────────────────────────────────────────────

    /// <summary>
    /// GetAllAsync phải nạp navigation property User cùng với LeaveRequest.
    /// Sau khi query, LeaveRequest.User không được null.
    /// </summary>
    [Test]
    public async Task GetAllAsync_IncludesUserNavigationProperty()
    {
        var (user, lr) = await SeedUserAndRequest();

        var result = (await _sut.GetAllAsync()).ToList();

        result.Should().HaveCount(1);
        result[0].User.Should().NotBeNull();
        result[0].User!.Email.Should().Be(user.Email);
    }

    /// <summary>
    /// GetAllAsync phải trả về đơn mới nhất trước (OrderByDescending CreatedAt).
    /// </summary>
    [Test]
    public async Task GetAllAsync_OrdersByCreatedAtDescending()
    {
        var user = User.Create("emp1", "emp1@clinic.com", "hash", UserRole.Staff);
        await _db.Users.AddAsync(user);
        var today = DateOnly.FromDateTime(DateTime.Today);
        var older = LeaveRequest.Create(user.Id, LeaveType.Annual, today, today.AddDays(1), "Cũ");
        var newer = LeaveRequest.Create(user.Id, LeaveType.Annual, today, today.AddDays(1), "Mới");
        typeof(LeaveRequest).GetProperty("CreatedAt")!.SetValue(older, DateTimeOffset.UtcNow.AddHours(-2));
        typeof(LeaveRequest).GetProperty("CreatedAt")!.SetValue(newer, DateTimeOffset.UtcNow);
        await _db.LeaveRequests.AddRangeAsync(older, newer);
        await _db.SaveChangesAsync();

        var result = (await _sut.GetAllAsync()).ToList();

        result[0].Id.Should().Be(newer.Id, because: "đơn mới nhất phải đứng đầu");
    }

    // ── GetByUserIdAsync ──────────────────────────────────────────────────────

    /// <summary>
    /// GetByUserIdAsync phải chỉ trả về đơn của user cụ thể đó.
    /// </summary>
    [Test]
    public async Task GetByUserIdAsync_ReturnsOnlyRequestsForSpecificUser()
    {
        var user1 = User.Create("emp1", "emp1@clinic.com", "hash", UserRole.Staff);
        var user2 = User.Create("emp2", "emp2@clinic.com", "hash", UserRole.Staff);
        await _db.Users.AddRangeAsync(user1, user2);
        var today = DateOnly.FromDateTime(DateTime.Today);
        await _db.LeaveRequests.AddRangeAsync(
            LeaveRequest.Create(user1.Id, LeaveType.Annual, today, today.AddDays(1), "U1 - 1"),
            LeaveRequest.Create(user1.Id, LeaveType.Annual, today, today.AddDays(2), "U1 - 2"),
            LeaveRequest.Create(user2.Id, LeaveType.Annual, today, today.AddDays(1), "U2 - 1")
        );
        await _db.SaveChangesAsync();

        var result = (await _sut.GetByUserIdAsync(user1.Id)).ToList();

        result.Should().HaveCount(2);
        result.Should().OnlyContain(lr => lr.UserId == user1.Id);
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────────

    /// <summary>
    /// GetByIdAsync phải trả về đúng đơn nghỉ và nạp navigation property User.
    /// </summary>
    [Test]
    public async Task GetByIdAsync_ExistingId_ReturnsWithUser()
    {
        var (user, lr) = await SeedUserAndRequest();

        var result = await _sut.GetByIdAsync(lr.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(lr.Id);
        result.User.Should().NotBeNull();
        result.User!.Email.Should().Be(user.Email);
    }

    /// <summary>
    /// GetByIdAsync với ID không tồn tại phải trả về null.
    /// </summary>
    [Test]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid());
        result.Should().BeNull();
    }

    // ── AddAsync ──────────────────────────────────────────────────────────────

    /// <summary>
    /// AddAsync phải lưu đơn và tự động nạp User navigation property
    /// (repository gọi db.Entry.Reference.LoadAsync sau SaveChanges).
    /// </summary>
    [Test]
    public async Task AddAsync_LoadsUserNavigationAfterSave()
    {
        var user = User.Create("emp1", "emp1@clinic.com", "hash", UserRole.Staff);
        await _db.Users.AddAsync(user);
        await _db.SaveChangesAsync();
        var today = DateOnly.FromDateTime(DateTime.Today);
        var lr = LeaveRequest.Create(user.Id, LeaveType.Annual, today, today.AddDays(2), "Test");

        await _sut.AddAsync(lr);

        lr.User.Should().NotBeNull(because: "AddAsync phải nạp User nav sau khi lưu");
        lr.User!.Email.Should().Be("emp1@clinic.com");
    }

    private async Task<(User user, LeaveRequest lr)> SeedUserAndRequest()
    {
        var user = User.Create("emp1", "emp1@clinic.com", "hash", UserRole.Staff);
        await _db.Users.AddAsync(user);
        var today = DateOnly.FromDateTime(DateTime.Today);
        var lr = LeaveRequest.Create(user.Id, LeaveType.Annual, today, today.AddDays(2), "Lý do test");
        await _db.LeaveRequests.AddAsync(lr);
        await _db.SaveChangesAsync();
        return (user, lr);
    }
}
