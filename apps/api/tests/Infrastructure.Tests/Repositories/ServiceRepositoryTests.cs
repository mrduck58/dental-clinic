using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Infrastructure.Persistence;
using DentalClinic.API.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Repositories;

[TestFixture]
public class ServiceRepositoryTests
{
    private AppDbContext _db = null!;
    private ServiceRepository _sut = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _sut = new ServiceRepository(_db);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    /// <summary>GetAllAsync phải trả về tất cả dịch vụ (cả đang tắt), sắp xếp theo tên A-Z.</summary>
    [Test]
    public async Task GetAllAsync_ReturnsAllServicesOrderedByName()
    {
        var b = Service.Create("Trồng răng", 5_000_000m, 60, "Mô tả");
        var a = Service.Create("Cạo vôi răng", 300_000m, 30, "Mô tả");
        a.SetActive(false);
        _db.Services.AddRange(b, a);
        await _db.SaveChangesAsync();

        var result = (await _sut.GetAllAsync()).ToList();

        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Cạo vôi răng");
        result[1].Name.Should().Be("Trồng răng");
    }

    /// <summary>GetByIdAsync với id tồn tại phải trả về đúng dịch vụ.</summary>
    [Test]
    public async Task GetByIdAsync_ExistingId_ReturnsService()
    {
        var service = Service.Create("Nhổ răng", 800_000m, 30, "Mô tả");
        _db.Services.Add(service);
        await _db.SaveChangesAsync();

        var result = await _sut.GetByIdAsync(service.Id);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Nhổ răng");
    }

    /// <summary>GetByIdAsync với id không tồn tại phải trả về null.</summary>
    [Test]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid());
        result.Should().BeNull();
    }

    /// <summary>AddAsync phải lưu dịch vụ mới vào DB.</summary>
    [Test]
    public async Task AddAsync_ValidService_PersistsToDatabase()
    {
        await _sut.AddAsync(Service.Create("Tẩy trắng răng", 1_500_000m, 90, "Mô tả"));

        (await _db.Services.CountAsync()).Should().Be(1);
    }

    /// <summary>UpdateAsync phải lưu lại thay đổi (ví dụ giá mới).</summary>
    [Test]
    public async Task UpdateAsync_ModifiedService_PersistsChanges()
    {
        var service = Service.Create("Trám răng", 500_000m, 30, "Mô tả");
        _db.Services.Add(service);
        await _db.SaveChangesAsync();

        service.Update("Trám răng thẩm mỹ", 700_000m, 45, "Mô tả mới", "", null, null);
        await _sut.UpdateAsync(service);

        var reloaded = await _db.Services.FindAsync(service.Id);
        reloaded!.Name.Should().Be("Trám răng thẩm mỹ");
        reloaded.Price.Should().Be(700_000m);
    }

    /// <summary>DeleteAsync phải xóa dịch vụ khỏi DB.</summary>
    [Test]
    public async Task DeleteAsync_ExistingService_RemovesFromDatabase()
    {
        var service = Service.Create("Niềng răng", 20_000_000m, 60, "Mô tả");
        _db.Services.Add(service);
        await _db.SaveChangesAsync();

        await _sut.DeleteAsync(service);

        (await _db.Services.CountAsync()).Should().Be(0);
    }

    /// <summary>GetActiveAsync phải chỉ trả về dịch vụ đang hoạt động, sắp xếp theo tên.</summary>
    [Test]
    public async Task GetActiveAsync_ExcludesInactiveServices()
    {
        var active = Service.Create("Khám tổng quát", 200_000m, 20, "Mô tả");
        var inactive = Service.Create("Dịch vụ cũ", 100_000m, 15, "Mô tả");
        inactive.SetActive(false);
        _db.Services.AddRange(active, inactive);
        await _db.SaveChangesAsync();

        var result = (await _sut.GetActiveAsync()).ToList();

        result.Should().ContainSingle(s => s.Id == active.Id);
    }
}
