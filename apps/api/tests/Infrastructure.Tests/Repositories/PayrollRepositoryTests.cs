using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Infrastructure.Persistence;
using DentalClinic.API.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Repositories;

[TestFixture]
public class PayrollRepositoryTests
{
    private AppDbContext _db = null!;
    private PayrollRepository _sut = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _sut = new PayrollRepository(_db);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    // ── GetPayableUsersAsync ──────────────────────────────────────────────────

    /// <summary>
    /// Bảng lương chỉ dành cho bác sĩ (Dentist/Doctor) và nhân viên (Staff).
    /// Tài khoản quản trị Admin/Owner và bệnh nhân không được xuất hiện trong kỳ lương.
    /// </summary>
    [Test]
    public async Task GetPayableUsersAsync_ReturnsOnlyDentistDoctorAndStaff()
    {
        await _db.Users.AddRangeAsync(
            User.Create("d1", "dentist@test.com", "hash", "Dentist"),
            User.Create("dr1", "doctor@test.com", "hash", "Doctor"),
            User.Create("s1", "staff@test.com", "hash", "Staff"),
            User.Create("a1", "admin@test.com", "hash", "Admin"),
            User.Create("o1", "owner@test.com", "hash", "Owner"),
            User.Create("p1", "patient@test.com", "hash", "Patient"));
        await _db.SaveChangesAsync();

        var result = await _sut.GetPayableUsersAsync();

        result.Select(u => u.Role).Should().BeEquivalentTo(["Dentist", "Doctor", "Staff"]);
    }

    // ── GetApprovedLeavesOverlappingAsync ─────────────────────────────────────

    /// <summary>
    /// Chỉ đơn nghỉ đã duyệt mới được tính vào lương; đơn Pending/Rejected/Cancelled bị loại.
    /// </summary>
    [Test]
    public async Task GetApprovedLeavesOverlappingAsync_ReturnsApprovedOnly()
    {
        var approved = MakeLeave(new DateOnly(2026, 8, 5), new DateOnly(2026, 8, 6));
        approved.Approve();
        var rejected = MakeLeave(new DateOnly(2026, 8, 7), new DateOnly(2026, 8, 8));
        rejected.Reject(null);
        var cancelled = MakeLeave(new DateOnly(2026, 8, 9), new DateOnly(2026, 8, 10));
        cancelled.Cancel();
        var pending = MakeLeave(new DateOnly(2026, 8, 11), new DateOnly(2026, 8, 12));

        await _db.LeaveRequests.AddRangeAsync(approved, rejected, cancelled, pending);
        await _db.SaveChangesAsync();

        var result = await _sut.GetApprovedLeavesOverlappingAsync(
            new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31));

        result.Should().ContainSingle().Which.Id.Should().Be(approved.Id);
    }

    /// <summary>
    /// Đơn nghỉ vắt qua biên kỳ (bắt đầu tháng trước, kết thúc trong kỳ) vẫn phải được lấy,
    /// vì phần ngày rơi vào kỳ vẫn bị tính lương.
    /// </summary>
    [Test]
    public async Task GetApprovedLeavesOverlappingAsync_LeaveSpanningPeriodBoundary_IsIncluded()
    {
        var spanning = MakeLeave(new DateOnly(2026, 7, 30), new DateOnly(2026, 8, 2));
        spanning.Approve();
        var outside = MakeLeave(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 3));
        outside.Approve();

        await _db.LeaveRequests.AddRangeAsync(spanning, outside);
        await _db.SaveChangesAsync();

        var result = await _sut.GetApprovedLeavesOverlappingAsync(
            new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31));

        result.Should().ContainSingle().Which.Id.Should().Be(spanning.Id);
    }

    // ── GetByPeriodAsync / GetByUserAndPeriodAsync ────────────────────────────

    /// <summary>Mỗi kỳ chỉ trả về đúng bản ghi của tháng/năm được hỏi.</summary>
    [Test]
    public async Task GetByPeriodAsync_ReturnsOnlyRecordsOfThatPeriod()
    {
        var userId = Guid.NewGuid();
        await _db.PayrollRecords.AddRangeAsync(
            PayrollRecord.Create(userId, 2026, 8, 10_000_000m, 0m, 0, 0m, 0m, 0m, 10_000_000m),
            PayrollRecord.Create(userId, 2026, 9, 10_000_000m, 0m, 0, 0m, 0m, 0m, 10_000_000m));
        await _db.SaveChangesAsync();

        var result = await _sut.GetByPeriodAsync(2026, 8);

        result.Should().ContainSingle().Which.Month.Should().Be(8);
    }

    private static LeaveRequest MakeLeave(DateOnly from, DateOnly to)
        => LeaveRequest.Create(Guid.NewGuid(), LeaveType.Annual, from, to, "Test");
}
