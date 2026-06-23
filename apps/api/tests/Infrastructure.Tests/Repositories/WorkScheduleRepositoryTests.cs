using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Infrastructure.Persistence;
using DentalClinic.API.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Repositories;

[TestFixture]
public class WorkScheduleRepositoryTests
{
    private AppDbContext _db = null!;
    private WorkScheduleRepository _sut = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _sut = new WorkScheduleRepository(_db);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    // ── GetByWeekAsync ────────────────────────────────────────────────────────

    /// <summary>
    /// GetByWeekAsync phải chỉ trả về các entry trong 7 ngày của tuần (weekStart..weekStart+6).
    /// Entry ngoài khoảng này không được xuất hiện trong kết quả.
    /// </summary>
    [Test]
    public async Task GetByWeekAsync_ReturnsOnlyEntriesInRange()
    {
        var weekStart = new DateOnly(2026, 6, 15);
        await _db.WorkSchedules.AddRangeAsync(
            MakeSchedule(weekStart),            // trong tuần
            MakeSchedule(weekStart.AddDays(6)), // cuối tuần — vẫn trong range
            MakeSchedule(weekStart.AddDays(7)), // tuần sau — ngoài range
            MakeSchedule(weekStart.AddDays(-1)) // tuần trước — ngoài range
        );
        await _db.SaveChangesAsync();

        var result = (await _sut.GetByWeekAsync(weekStart)).ToList();

        result.Should().HaveCount(2);
        result.Should().OnlyContain(s => s.Date >= weekStart && s.Date <= weekStart.AddDays(6));
    }

    /// <summary>
    /// Kết quả phải được sắp xếp theo Date tăng dần, rồi đến Shift — để hiển thị lịch đúng thứ tự.
    /// </summary>
    [Test]
    public async Task GetByWeekAsync_OrdersByDateThenShift()
    {
        var weekStart = new DateOnly(2026, 6, 15);
        await _db.WorkSchedules.AddRangeAsync(
            MakeSchedule(weekStart.AddDays(1), "Chiều"),
            MakeSchedule(weekStart, "Sáng"),
            MakeSchedule(weekStart, "Chiều")
        );
        await _db.SaveChangesAsync();

        var result = (await _sut.GetByWeekAsync(weekStart)).ToList();

        result[0].Date.Should().Be(weekStart);
        result[0].Shift.Should().Be("Chiều");
        result[1].Date.Should().Be(weekStart);
        result[1].Shift.Should().Be("Sáng");
        result[2].Date.Should().Be(weekStart.AddDays(1));
    }

    // ── ReplaceWeekAsync ──────────────────────────────────────────────────────

    /// <summary>
    /// ReplaceWeekAsync phải xóa toàn bộ lịch cũ của tuần và thay bằng lịch mới.
    /// </summary>
    [Test]
    public async Task ReplaceWeekAsync_DeletesExistingAndAddsNew()
    {
        var weekStart = new DateOnly(2026, 6, 15);
        await _db.WorkSchedules.AddRangeAsync(
            MakeSchedule(weekStart, "Sáng"),
            MakeSchedule(weekStart.AddDays(1), "Chiều")
        );
        await _db.SaveChangesAsync();

        var newEntries = new[]
        {
            MakeSchedule(weekStart, "Sáng", "Bs. Mới 1"),
            MakeSchedule(weekStart, "Chiều", "Bs. Mới 2"),
            MakeSchedule(weekStart.AddDays(2), "Sáng", "Bs. Mới 3"),
        };
        await _sut.ReplaceWeekAsync(weekStart, newEntries);

        var stored = (await _sut.GetByWeekAsync(weekStart)).ToList();
        stored.Should().HaveCount(3);
        stored.Should().OnlyContain(s => s.StaffName.Contains("Mới"));
    }

    /// <summary>
    /// Khi truyền danh sách entry rỗng, toàn bộ lịch cũ của tuần phải bị xóa.
    /// </summary>
    [Test]
    public async Task ReplaceWeekAsync_EmptyEntries_ClearsExistingWeek()
    {
        var weekStart = new DateOnly(2026, 6, 15);
        await _db.WorkSchedules.AddRangeAsync(
            MakeSchedule(weekStart),
            MakeSchedule(weekStart.AddDays(1))
        );
        await _db.SaveChangesAsync();

        await _sut.ReplaceWeekAsync(weekStart, Enumerable.Empty<WorkSchedule>());

        var stored = await _sut.GetByWeekAsync(weekStart);
        stored.Should().BeEmpty();
    }

    /// <summary>
    /// ReplaceWeekAsync không được ảnh hưởng đến lịch của tuần khác.
    /// </summary>
    [Test]
    public async Task ReplaceWeekAsync_DoesNotAffectOtherWeeks()
    {
        var weekStart = new DateOnly(2026, 6, 15);
        var nextWeek  = weekStart.AddDays(7);
        await _db.WorkSchedules.AddAsync(MakeSchedule(nextWeek));
        await _db.SaveChangesAsync();

        await _sut.ReplaceWeekAsync(weekStart, Enumerable.Empty<WorkSchedule>());

        var nextWeekSchedules = await _sut.GetByWeekAsync(nextWeek);
        nextWeekSchedules.Should().HaveCount(1);
    }

    private static WorkSchedule MakeSchedule(DateOnly date, string shift = "Sáng", string staffName = "Bs. Test")
        => WorkSchedule.Create(date, shift, "Khám", "Nha sĩ", staffName, "Phòng 1", "#FFFFFF", false);
}
