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
    /// Bảng lương chỉ dành cho bác sĩ (Dentist) và nhân viên (Staff).
    /// Tài khoản quản trị Admin/Owner và bệnh nhân không được xuất hiện trong kỳ lương.
    /// </summary>
    [Test]
    public async Task GetPayableUsersAsync_ReturnsOnlyDentistAndStaff()
    {
        await _db.Users.AddRangeAsync(
            User.Create("d1", "dentist@test.com", "hash", UserRole.Dentist),
            User.Create("s1", "staff@test.com", "hash", UserRole.Staff),
            User.Create("a1", "admin@test.com", "hash", UserRole.Admin),
            User.Create("o1", "owner@test.com", "hash", UserRole.Owner),
            User.Create("p1", "patient@test.com", "hash", UserRole.Patient));
        await _db.SaveChangesAsync();

        var result = await _sut.GetPayableUsersAsync();

        result.Select(u => u.Role).Should().BeEquivalentTo([UserRole.Dentist, UserRole.Staff]);
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
            PayrollRecord.CreateDraft(userId, 2026, 8, 10_000_000m, 0m, 0, 0, 0m, 0m, 0m),
            PayrollRecord.CreateDraft(userId, 2026, 9, 10_000_000m, 0m, 0, 0, 0m, 0m, 0m));
        await _db.SaveChangesAsync();

        var result = await _sut.GetByPeriodAsync(2026, 8);

        result.Should().ContainSingle().Which.Month.Should().Be(8);
    }

    private static LeaveRequest MakeLeave(DateOnly from, DateOnly to)
    {
        var shifts = new List<(DateOnly Date, string ShiftId)>();
        for (var d = from; d <= to; d = d.AddDays(1))
            shifts.Add((d, "08:00-10:00"));

        return LeaveRequest.Create(Guid.NewGuid(), LeaveType.Annual, shifts, "Test");
    }
}
