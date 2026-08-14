using DentalClinic.API.Application.DTOs.Services;
using DentalClinic.API.Application.UseCases.Services;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Infrastructure.Persistence;
using DentalClinic.API.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Handlers;

/// <summary>
/// Cập nhật dịch vụ CÓ tuỳ chọn — chạy trên repository thật để bắt lỗi ở tầng theo dõi thay đổi của
/// EF, thứ mà test dùng mock repository không thể lộ ra.
/// </summary>
[TestFixture]
public class UpdateServiceWithOptionsTests
{
    private AppDbContext _db = null!;
    private ServiceRepository _repo = null!;
    private UpdateServiceHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

        _repo = new ServiceRepository(_db);
        _handler = new UpdateServiceHandler(
            _repo, Substitute.For<IActivityLogService>(), Substitute.For<ICurrentUserService>());
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    private async Task<Service> SeedServiceWithOptionsAsync()
    {
        var service = Service.Create("Trám răng", 300_000m, 30, "Mô tả", "", null, null);
        service.ReplaceOptions([("Răng cửa", 300_000m, "Răng", 0)]);
        _db.Services.Add(service);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();
        return service;
    }

    private UpdateServiceCommand CommandFor(Guid id, params string[] optionNames) =>
        new(id, "Trám răng thẩm mỹ", 500_000m, 45, "Mô tả mới", "", null, null,
            optionNames.Select((n, i) => new ServiceOptionRequest(n, 500_000m, "Răng", i)).ToList());

    /// <summary>
    /// Đây là lỗi khiến "cập nhật dịch vụ" hỏng: handler xoá hết tuỳ chọn cũ, thêm tuỳ chọn mới
    /// (ServiceOption.Create TỰ SINH Guid), rồi gọi db.Services.Update(service).
    ///
    /// DbSet.Update đánh dấu MỌI entity trong đồ thị có khoá đã set là Modified — nên các tuỳ chọn
    /// vừa tạo bị coi là Modified thay vì Added. EF phát câu UPDATE cho hàng chưa hề tồn tại,
    /// 0 dòng bị ảnh hưởng, và ném DbUpdateConcurrencyException (ExceptionMiddleware không map
    /// loại này nên người dùng nhận 500).
    /// </summary>
    [Test]
    public async Task Handle_ServiceWithOptions_SavesWithoutConcurrencyError()
    {
        var service = await SeedServiceWithOptionsAsync();

        Func<Task> act = () => _handler.Handle(CommandFor(service.Id, "Răng hàm"), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task Handle_ReplacesOptionsWithTheNewSet()
    {
        var service = await SeedServiceWithOptionsAsync();

        await _handler.Handle(CommandFor(service.Id, "Răng hàm", "Răng nanh"), CancellationToken.None);

        var saved = await _db.Services.Include(s => s.Options).SingleAsync(s => s.Id == service.Id);
        saved.Options.Select(o => o.Name).Should().BeEquivalentTo("Răng hàm", "Răng nanh");
    }

    [Test]
    public async Task Handle_UpdatesScalarFields()
    {
        var service = await SeedServiceWithOptionsAsync();

        await _handler.Handle(CommandFor(service.Id, "Răng hàm"), CancellationToken.None);

        var saved = await _db.Services.SingleAsync(s => s.Id == service.Id);
        saved.Name.Should().Be("Trám răng thẩm mỹ");
        saved.Price.Should().Be(500_000m);
    }

    /// <summary>Bỏ hết tuỳ chọn cũng phải lưu được, không sót bản ghi mồ côi.</summary>
    [Test]
    public async Task Handle_RemovingAllOptions_LeavesNone()
    {
        var service = await SeedServiceWithOptionsAsync();

        await _handler.Handle(CommandFor(service.Id), CancellationToken.None);

        var saved = await _db.Services.Include(s => s.Options).SingleAsync(s => s.Id == service.Id);
        saved.Options.Should().BeEmpty();
    }
}
