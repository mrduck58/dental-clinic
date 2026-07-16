using DentalClinic.API.Application.UseCases.Inventory;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Handlers;

[TestFixture]
public class GetMaterialRequestsHandlerTests
{
    private AppDbContext _db = null!;
    private GetMaterialRequestsHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _handler = new GetMaterialRequestsHandler(_db);
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    /// <summary>Không truyền trạng thái lọc phải trả về toàn bộ yêu cầu, mới nhất trước.</summary>
    [Test]
    public async Task HandleAsync_NoStatusFilter_ReturnsAllOrderedByNewestFirst()
    {
        var older = MaterialRequest.Create(Guid.NewGuid(), "Niềng răng", "Bệnh nhân A", "BS X", "Cần thêm khay niềng");
        var newer = MaterialRequest.Create(Guid.NewGuid(), "Trồng Implant", "Bệnh nhân B", "BS Y", "Cần trụ implant");
        _db.MaterialRequests.AddRange(older, newer);
        await _db.SaveChangesAsync();

        var result = await _handler.HandleAsync(null);

        result.Should().HaveCount(2);
        result[0].Id.Should().Be(newer.Id);
    }

    /// <summary>Lọc theo trạng thái "Done" phải chỉ trả về các yêu cầu đã xử lý.</summary>
    [Test]
    public async Task HandleAsync_FilterByDoneStatus_ReturnsOnlyDoneRequests()
    {
        var pending = MaterialRequest.Create(Guid.NewGuid(), "Niềng răng", "Bệnh nhân A", "BS X", "Yêu cầu 1");
        var done = MaterialRequest.Create(Guid.NewGuid(), "Trồng Implant", "Bệnh nhân B", "BS Y", "Yêu cầu 2");
        done.MarkDone("staff1");
        _db.MaterialRequests.AddRange(pending, done);
        await _db.SaveChangesAsync();

        var result = await _handler.HandleAsync("Done");

        result.Should().ContainSingle(r => r.Id == done.Id);
    }

    /// <summary>Giá trị trạng thái không hợp lệ (không parse được enum) phải bị bỏ qua, không lọc gì.</summary>
    [Test]
    public async Task HandleAsync_InvalidStatusValue_IgnoresFilterAndReturnsAll()
    {
        var request = MaterialRequest.Create(Guid.NewGuid(), "Niềng răng", "Bệnh nhân A", "BS X", "Yêu cầu 1");
        _db.MaterialRequests.Add(request);
        await _db.SaveChangesAsync();

        var result = await _handler.HandleAsync("not-a-real-status");

        result.Should().ContainSingle();
    }

    /// <summary>Lọc theo trạng thái "Pending" phải chỉ trả về các yêu cầu chưa xử lý.</summary>
    [Test]
    public async Task HandleAsync_FilterByPendingStatus_ReturnsOnlyPendingRequests()
    {
        var pending = MaterialRequest.Create(Guid.NewGuid(), "Niềng răng", "Bệnh nhân A", "BS X", "Yêu cầu 1");
        var done = MaterialRequest.Create(Guid.NewGuid(), "Trồng Implant", "Bệnh nhân B", "BS Y", "Yêu cầu 2");
        done.MarkDone("staff1");
        _db.MaterialRequests.AddRange(pending, done);
        await _db.SaveChangesAsync();

        var result = await _handler.HandleAsync("Pending");

        result.Should().ContainSingle(r => r.Id == pending.Id);
    }

    /// <summary>Giá trị trạng thái lọc không phân biệt hoa/thường (vd "done" viết thường) vẫn phải khớp đúng enum.</summary>
    [Test]
    public async Task HandleAsync_StatusFilterLowerCase_MatchesEnumCaseInsensitively()
    {
        var pending = MaterialRequest.Create(Guid.NewGuid(), "Niềng răng", "Bệnh nhân A", "BS X", "Yêu cầu 1");
        var done = MaterialRequest.Create(Guid.NewGuid(), "Trồng Implant", "Bệnh nhân B", "BS Y", "Yêu cầu 2");
        done.MarkDone("staff1");
        _db.MaterialRequests.AddRange(pending, done);
        await _db.SaveChangesAsync();

        var result = await _handler.HandleAsync("done");

        result.Should().ContainSingle(r => r.Id == done.Id);
    }

    /// <summary>Trạng thái lọc chỉ gồm khoảng trắng phải được coi như không lọc, trả về toàn bộ.</summary>
    [Test]
    public async Task HandleAsync_WhitespaceStatusFilter_IgnoresFilterAndReturnsAll()
    {
        var pending = MaterialRequest.Create(Guid.NewGuid(), "Niềng răng", "Bệnh nhân A", "BS X", "Yêu cầu 1");
        var done = MaterialRequest.Create(Guid.NewGuid(), "Trồng Implant", "Bệnh nhân B", "BS Y", "Yêu cầu 2");
        done.MarkDone("staff1");
        _db.MaterialRequests.AddRange(pending, done);
        await _db.SaveChangesAsync();

        var result = await _handler.HandleAsync("   ");

        result.Should().HaveCount(2);
    }
}
