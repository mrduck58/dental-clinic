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
public class MarkMaterialRequestOrderedHandlerTests
{
    private AppDbContext _db = null!;
    private MarkMaterialRequestOrderedHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _handler = new MarkMaterialRequestOrderedHandler(new MaterialRequestRepository(_db));
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    [Test]
    public async Task HandleAsync_RequestNotFound_ThrowsNotFoundException()
    {
        Func<Task> act = () => _handler.Handle(new MarkMaterialRequestOrderedCommand(Guid.NewGuid(), "staff1", null), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task HandleAsync_PendingRequest_TransitionsToOrderedWithNoteAndTimestamp()
    {
        var request = MaterialRequest.Create("Niềng răng", "Bệnh nhân A", "BS X", [("Khay niềng", 1, "Cái")]);
        _db.MaterialRequests.Add(request);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new MarkMaterialRequestOrderedCommand(request.Id, "staff1", "Đặt lab ABC"), CancellationToken.None);

        result.Status.Should().Be(MaterialRequestStatus.Ordered.ToString());
        result.OrderedBy.Should().Be("staff1");
        result.SupplierNote.Should().Be("Đặt lab ABC");

        var updated = await _db.MaterialRequests.SingleAsync(r => r.Id == request.Id);
        updated.Status.Should().Be(MaterialRequestStatus.Ordered);
        updated.OrderedAt.Should().NotBeNull();
    }

    /// <summary>Không thể đặt hàng lại cho yêu cầu đã ở trạng thái Ordered hoặc Done — chỉ Pending mới được.</summary>
    [Test]
    public async Task HandleAsync_AlreadyOrderedRequest_ThrowsValidationException()
    {
        var request = MaterialRequest.Create("Niềng răng", "Bệnh nhân A", "BS X", [("Khay niềng", 1, "Cái")]);
        request.MarkOrdered("staff1", null);
        _db.MaterialRequests.Add(request);
        await _db.SaveChangesAsync();

        Func<Task> act = () => _handler.Handle(new MarkMaterialRequestOrderedCommand(request.Id, "staff2", null), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task HandleAsync_DoneRequest_ThrowsValidationException()
    {
        var request = MaterialRequest.Create("Niềng răng", "Bệnh nhân A", "BS X", [("Khay niềng", 1, "Cái")]);
        request.MarkDone("staff1");
        _db.MaterialRequests.Add(request);
        await _db.SaveChangesAsync();

        Func<Task> act = () => _handler.Handle(new MarkMaterialRequestOrderedCommand(request.Id, "staff2", null), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }
}
