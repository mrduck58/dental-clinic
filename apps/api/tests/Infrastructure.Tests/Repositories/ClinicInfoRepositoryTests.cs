using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Infrastructure.Persistence;
using DentalClinic.API.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Repositories;

[TestFixture]
public class ClinicInfoRepositoryTests
{
    private AppDbContext _db = null!;
    private ClinicInfoRepository _sut = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _sut = new ClinicInfoRepository(_db);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    /// <summary>Chưa có ClinicInfo nào phải trả về null.</summary>
    [Test]
    public async Task GetAsync_NoRecord_ReturnsNull()
    {
        var result = await _sut.GetAsync();
        result.Should().BeNull();
    }

    /// <summary>AddAsync phải lưu bản ghi, GetAsync đọc lại được đúng dữ liệu.</summary>
    [Test]
    public async Task AddAsync_ThenGetAsync_ReturnsSavedRecord()
    {
        var info = MakeClinicInfo("Sơn Giang Dental");

        await _sut.AddAsync(info);
        var result = await _sut.GetAsync();

        result.Should().NotBeNull();
        result!.AboutTitle.Should().Be("Sơn Giang Dental");
    }

    /// <summary>Có nhiều bản ghi thì GetAsync phải trả về bản ghi được tạo sớm nhất.</summary>
    [Test]
    public async Task GetAsync_MultipleRecords_ReturnsEarliestCreated()
    {
        var older = MakeClinicInfo("Bản cũ");
        var newer = MakeClinicInfo("Bản mới");
        _db.ClinicInfos.AddRange(older, newer);
        await _db.SaveChangesAsync();

        var result = await _sut.GetAsync();

        result!.Id.Should().Be(older.Id);
    }

    /// <summary>UpdateAsync phải lưu lại thay đổi vào DB.</summary>
    [Test]
    public async Task UpdateAsync_ModifiedRecord_PersistsChanges()
    {
        var info = MakeClinicInfo("Tên cũ");
        _db.ClinicInfos.Add(info);
        await _db.SaveChangesAsync();

        info.UpdateContent("Tên mới", info.AboutDescription, info.FoundedYear, info.Phone, info.Email, info.Address, info.AboutImageUrl, info.WorkingHours);
        await _sut.UpdateAsync(info);

        var reloaded = await _db.ClinicInfos.FindAsync(info.Id);
        reloaded!.AboutTitle.Should().Be("Tên mới");
    }

    private static ClinicInfo MakeClinicInfo(string title)
        => ClinicInfo.Create(title, "Mô tả", 2020, "0900000000", "clinic@test.com", "123 Đường Test");
}
