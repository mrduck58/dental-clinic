using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Infrastructure.Persistence;
using DentalClinic.API.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Repositories;

[TestFixture]
public class UserRepositoryTests
{
    private AppDbContext _db = null!;
    private UserRepository _sut = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _sut = new UserRepository(_db);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    // ── GetByEmailAsync ───────────────────────────────────────────────────────

    /// <summary>
    /// Email tồn tại trong DB phải trả về đúng user đó.
    /// </summary>
    [Test]
    public async Task GetByEmailAsync_ExistingEmail_ReturnsUser()
    {
        var user = User.Create("emp1", "emp1@clinic.com", "hash", UserRole.Staff);
        await _db.Users.AddAsync(user);
        await _db.SaveChangesAsync();

        var result = await _sut.GetByEmailAsync("emp1@clinic.com");

        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
    }

    /// <summary>
    /// Email không tồn tại phải trả về null.
    /// </summary>
    [Test]
    public async Task GetByEmailAsync_NonExistingEmail_ReturnsNull()
    {
        var result = await _sut.GetByEmailAsync("notfound@clinic.com");
        result.Should().BeNull();
    }

    // ── ExistsByEmailAsync ────────────────────────────────────────────────────

    /// <summary>
    /// Email đã có trong DB phải trả về true.
    /// </summary>
    [Test]
    public async Task ExistsByEmailAsync_ExistingEmail_ReturnsTrue()
    {
        await _db.Users.AddAsync(User.Create("emp1", "emp1@clinic.com", "hash", UserRole.Staff));
        await _db.SaveChangesAsync();

        var result = await _sut.ExistsByEmailAsync("emp1@clinic.com");

        result.Should().BeTrue();
    }

    /// <summary>
    /// Email chưa có trong DB phải trả về false.
    /// </summary>
    [Test]
    public async Task ExistsByEmailAsync_NonExistingEmail_ReturnsFalse()
    {
        var result = await _sut.ExistsByEmailAsync("new@clinic.com");
        result.Should().BeFalse();
    }

    // ── ExistsByUsernameAsync ─────────────────────────────────────────────────

    /// <summary>
    /// Username đã có trong DB phải trả về true.
    /// </summary>
    [Test]
    public async Task ExistsByUsernameAsync_ExistingUsername_ReturnsTrue()
    {
        await _db.Users.AddAsync(User.Create("bsnguyen", "nguyen@clinic.com", "hash", UserRole.Dentist));
        await _db.SaveChangesAsync();

        var result = await _sut.ExistsByUsernameAsync("bsnguyen");

        result.Should().BeTrue();
    }

    // ── GetStaffPagedAsync ────────────────────────────────────────────────────

    /// <summary>
    /// Tìm kiếm theo tên (FullName) không phân biệt hoa thường phải trả về đúng user.
    /// </summary>
    [Test]
    public async Task GetStaffPagedAsync_SearchByFullName_ReturnsMatchingUsers()
    {
        await _db.Users.AddRangeAsync(
            User.Create("emp1", "a@clinic.com", "hash", UserRole.Staff, null, "Nguyễn Văn An"),
            User.Create("emp2", "b@clinic.com", "hash", UserRole.Staff, null, "Trần Thị Bình"),
            User.Create("emp3", "c@clinic.com", "hash", UserRole.Dentist, null, "Nguyễn Thị Cúc"));
        await _db.SaveChangesAsync();

        var (items, total) = await _sut.GetStaffPagedAsync("nguyễn", null, null, 1, 10);

        total.Should().Be(2);
        items.Should().OnlyContain(u => u.FullName!.Contains("Nguyễn", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Filter theo role phải chỉ trả về user có đúng role đó (không tính Patient).
    /// </summary>
    [Test]
    public async Task GetStaffPagedAsync_FilterByRole_ReturnsOnlyThatRole()
    {
        await _db.Users.AddRangeAsync(
            User.Create("d1", "d1@clinic.com", "hash", UserRole.Dentist),
            User.Create("s1", "s1@clinic.com", "hash", UserRole.Staff),
            User.Create("s2", "s2@clinic.com", "hash", UserRole.Staff));
        await _db.SaveChangesAsync();

        var (items, total) = await _sut.GetStaffPagedAsync(null, "Staff", null, 1, 10);

        total.Should().Be(2);
        items.Should().OnlyContain(u => u.Role == UserRole.Staff);
    }

    /// <summary>
    /// Phân trang phải trả về đúng số lượng item và tổng count.
    /// </summary>
    [Test]
    public async Task GetStaffPagedAsync_Pagination_ReturnsCorrectCounts()
    {
        for (var i = 1; i <= 5; i++)
            await _db.Users.AddAsync(User.Create($"emp{i}", $"emp{i}@clinic.com", "hash", UserRole.Staff));
        await _db.SaveChangesAsync();

        var (page1, total) = await _sut.GetStaffPagedAsync(null, null, null, 1, 3);
        var (page2, _)     = await _sut.GetStaffPagedAsync(null, null, null, 2, 3);

        total.Should().Be(5);
        page1.Should().HaveCount(3);
        page2.Should().HaveCount(2);
    }

    // ── GetStaffStatsAsync ────────────────────────────────────────────────────

    /// <summary>
    /// Stats phải đếm đúng số lượng Dentist và tổng nhân viên (Dentist+Staff).
    /// Vai trò "Doctor" không còn tồn tại trong hệ thống, nên TotalDoctors luôn bằng 0.
    /// </summary>
    [Test]
    public async Task GetStaffStatsAsync_CountsCorrectlyByRole()
    {
        await _db.Users.AddRangeAsync(
            User.Create("d1", "d1@clinic.com", "hash", UserRole.Dentist),
            User.Create("d2", "d2@clinic.com", "hash", UserRole.Dentist),
            User.Create("d3", "d3@clinic.com", "hash", UserRole.Dentist),
            User.Create("s1", "s1@clinic.com", "hash", UserRole.Staff),
            User.Create("p1", "p1@clinic.com", "hash", UserRole.Patient));
        await _db.SaveChangesAsync();

        var stats = await _sut.GetStaffStatsAsync();

        stats.TotalDentists.Should().Be(3);
        stats.TotalDoctors.Should().Be(0);
        stats.TotalEmployees.Should().Be(4);
    }
}
