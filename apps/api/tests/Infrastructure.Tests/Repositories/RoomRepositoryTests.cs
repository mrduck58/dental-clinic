using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Infrastructure.Persistence;
using DentalClinic.API.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Repositories;

[TestFixture]
public class RoomRepositoryTests
{
    private AppDbContext _db = null!;
    private RoomRepository _sut = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _sut = new RoomRepository(_db);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    // ── GetAllAsync ───────────────────────────────────────────────────────────

    /// <summary>
    /// GetAllAsync phải trả về phòng theo thứ tự: tầng tăng dần, rồi code tăng dần.
    /// </summary>
    [Test]
    public async Task GetAllAsync_OrdersByFloorThenCode()
    {
        await _db.Rooms.AddRangeAsync(
            MakeRoom("P03", "Tầng 2"),
            MakeRoom("P01", "Tầng 1"),
            MakeRoom("P02", "Tầng 1")
        );
        await _db.SaveChangesAsync();

        var result = (await _sut.GetAllAsync()).ToList();

        result[0].Code.Should().Be("P01");
        result[1].Code.Should().Be("P02");
        result[2].Code.Should().Be("P03");
    }

    // ── ExistsByCodeAsync ─────────────────────────────────────────────────────

    /// <summary>
    /// Code đã tồn tại trong DB phải trả về true.
    /// </summary>
    [Test]
    public async Task ExistsByCodeAsync_ExistingCode_ReturnsTrue()
    {
        await _db.Rooms.AddAsync(MakeRoom("P001"));
        await _db.SaveChangesAsync();

        var result = await _sut.ExistsByCodeAsync("P001");

        result.Should().BeTrue();
    }

    /// <summary>
    /// Code không tồn tại phải trả về false.
    /// </summary>
    [Test]
    public async Task ExistsByCodeAsync_NonExistingCode_ReturnsFalse()
    {
        var result = await _sut.ExistsByCodeAsync("P999");
        result.Should().BeFalse();
    }

    /// <summary>
    /// Khi truyền excludeId là chính phòng đó, phải trả về false (dùng cho Update — không tính chính nó).
    /// </summary>
    [Test]
    public async Task ExistsByCodeAsync_WithExcludeId_ExcludesSpecifiedRoom()
    {
        var room = MakeRoom("P001");
        await _db.Rooms.AddAsync(room);
        await _db.SaveChangesAsync();

        var result = await _sut.ExistsByCodeAsync("P001", excludeId: room.Id);

        result.Should().BeFalse(because: "phòng đang cập nhật không tính là trùng với chính nó");
    }

    // ── ExistsByNameAsync ─────────────────────────────────────────────────────

    /// <summary>
    /// Tên đã tồn tại trong DB phải trả về true.
    /// </summary>
    [Test]
    public async Task ExistsByNameAsync_ExistingName_ReturnsTrue()
    {
        await _db.Rooms.AddAsync(MakeRoom("P001", name: "Phòng Nha Khoa 1"));
        await _db.SaveChangesAsync();

        var result = await _sut.ExistsByNameAsync("Phòng Nha Khoa 1");

        result.Should().BeTrue();
    }

    /// <summary>
    /// So sánh tên không phân biệt hoa thường (ExistsByNameAsync dùng ToLower()).
    /// </summary>
    [Test]
    public async Task ExistsByNameAsync_CaseInsensitive_ReturnsTrue()
    {
        await _db.Rooms.AddAsync(MakeRoom("P001", name: "Phòng Nha Khoa 1"));
        await _db.SaveChangesAsync();

        var result = await _sut.ExistsByNameAsync("phòng nha khoa 1");

        result.Should().BeTrue();
    }

    /// <summary>
    /// Khi truyền excludeId là chính phòng đó, phải trả về false (dùng cho Update).
    /// </summary>
    [Test]
    public async Task ExistsByNameAsync_WithExcludeId_ExcludesSpecifiedRoom()
    {
        var room = MakeRoom("P001", name: "Phòng Nha Khoa 1");
        await _db.Rooms.AddAsync(room);
        await _db.SaveChangesAsync();

        var result = await _sut.ExistsByNameAsync("Phòng Nha Khoa 1", excludeId: room.Id);

        result.Should().BeFalse();
    }

    private static Room MakeRoom(string code, string floor = "Tầng 1", string name = "Phòng Test")
        => Room.Create(code, name, floor, "Phòng khám", "Mô tả");
}
