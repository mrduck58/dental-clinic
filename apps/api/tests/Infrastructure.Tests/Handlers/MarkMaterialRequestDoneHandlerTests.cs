using DentalClinic.API.Application.UseCases.Inventory;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Infrastructure.Persistence;
using DentalClinic.API.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Handlers;

[TestFixture]
public class MarkMaterialRequestDoneHandlerTests
{
    private AppDbContext _db = null!;
    private MarkMaterialRequestDoneHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _handler = new MarkMaterialRequestDoneHandler(new MaterialRequestRepository(_db));
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    /// <summary>Yêu cầu vật tư không tồn tại phải báo lỗi NotFoundException.</summary>
    [Test]
    public async Task HandleAsync_RequestNotFound_ThrowsNotFoundException()
    {
        Func<Task> act = () => _handler.Handle(new MarkMaterialRequestDoneCommand(Guid.NewGuid(), "staff1"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>Đánh dấu hoàn tất hợp lệ phải cập nhật trạng thái, thời gian và người xử lý.</summary>
    [Test]
    public async Task HandleAsync_ValidRequest_MarksAsDoneWithHandlerInfo()
    {
        var request = MaterialRequest.Create("Niềng răng", "Bệnh nhân A", "BS X", "Cần thêm khay niềng");
        _db.MaterialRequests.Add(request);
        await _db.SaveChangesAsync();

        await _handler.Handle(new MarkMaterialRequestDoneCommand(request.Id, "staff1"), CancellationToken.None);

        var updated = await _db.MaterialRequests.SingleAsync(r => r.Id == request.Id);
        updated.Status.Should().Be(MaterialRequestStatus.Done);
        updated.HandledBy.Should().Be("staff1");
        updated.HandledAt.Should().NotBeNull();
    }

    /// <summary>Đánh dấu hoàn tất một yêu cầu đã hoàn tất trước đó phải ghi đè người xử lý bằng giá trị mới nhất
    /// (handler không có bảo vệ chống xử lý lại/idempotency).</summary>
    [Test]
    public async Task HandleAsync_AlreadyDoneRequest_OverwritesHandledByWithLatestValue()
    {
        var request = MaterialRequest.Create("Niềng răng", "Bệnh nhân A", "BS X", "Cần thêm khay niềng");
        request.MarkDone("staff1");
        _db.MaterialRequests.Add(request);
        await _db.SaveChangesAsync();

        await _handler.Handle(new MarkMaterialRequestDoneCommand(request.Id, "staff2"), CancellationToken.None);

        var updated = await _db.MaterialRequests.SingleAsync(r => r.Id == request.Id);
        updated.HandledBy.Should().Be("staff2");
    }
}
