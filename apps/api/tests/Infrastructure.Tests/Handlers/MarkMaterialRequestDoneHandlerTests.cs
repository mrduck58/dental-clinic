using DentalClinic.API.Application.DTOs.Inventory;
using DentalClinic.API.Application.UseCases.Inventory;
using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Infrastructure.Persistence;
using DentalClinic.API.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Handlers;

/// <summary>
/// StockImportHandler (được gọi qua ISender cho từng item) đã có test riêng trong
/// <see cref="StockImportHandlerTests"/> — ở đây chỉ mock ISender để xác nhận
/// MarkMaterialRequestDoneHandler uỷ quyền đúng tham số cho từng item và cập nhật đúng trạng thái/liên kết.
/// </summary>
[TestFixture]
public class MarkMaterialRequestDoneHandlerTests
{
    private AppDbContext _db = null!;
    private ISender _sender = null!;
    private MarkMaterialRequestDoneHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _sender = Substitute.For<ISender>();
        _sender.Send(Arg.Any<StockImportCommand>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var cmd = ci.Arg<StockImportCommand>();
                return new SupplyTransactionDto(Guid.NewGuid(), Guid.NewGuid(), cmd.Name, "import", cmd.Quantity, cmd.UnitPrice, cmd.Note, cmd.CreatedBy, DateTimeOffset.UtcNow);
            });
        _handler = new MarkMaterialRequestDoneHandler(new MaterialRequestRepository(_db), _sender);
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    /// <summary>Yêu cầu vật tư không tồn tại phải báo lỗi NotFoundException.</summary>
    [Test]
    public async Task HandleAsync_RequestNotFound_ThrowsNotFoundException()
    {
        Func<Task> act = () => _handler.Handle(new MarkMaterialRequestDoneCommand(Guid.NewGuid(), "staff1", []), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>Thiếu giá cho 1 item phải bị từ chối, KHÔNG nhập kho item nào và KHÔNG đánh dấu Done
    /// (rollback đúng khi validate fail giữa chừng).</summary>
    [Test]
    public async Task HandleAsync_MissingPriceForAnItem_ThrowsValidationExceptionAndImportsNothing()
    {
        var request = MaterialRequest.Create("Niềng răng", "Bệnh nhân A", "BS X",
            [("Khay niềng", 1, "Cái"), ("Chỉ khâu 4/0", 2, "Cuộn")]);
        _db.MaterialRequests.Add(request);
        await _db.SaveChangesAsync();
        var khayNiengId = request.Items.First(i => i.ItemName == "Khay niềng").Id;

        Func<Task> act = () => _handler.Handle(
            new MarkMaterialRequestDoneCommand(request.Id, "staff1", [new MaterialRequestItemPriceInput(khayNiengId, 10_000m)]),
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        await _sender.DidNotReceive().Send(Arg.Any<StockImportCommand>(), Arg.Any<CancellationToken>());
        var unchanged = await _db.MaterialRequests.SingleAsync(r => r.Id == request.Id);
        unchanged.Status.Should().Be(MaterialRequestStatus.Pending);
    }

    /// <summary>Giá âm phải bị từ chối.</summary>
    [Test]
    public async Task HandleAsync_NegativePrice_ThrowsValidationException()
    {
        var request = MaterialRequest.Create("Niềng răng", "Bệnh nhân A", "BS X", [("Khay niềng", 1, "Cái")]);
        _db.MaterialRequests.Add(request);
        await _db.SaveChangesAsync();
        var itemId = request.Items.First().Id;

        Func<Task> act = () => _handler.Handle(
            new MarkMaterialRequestDoneCommand(request.Id, "staff1", [new MaterialRequestItemPriceInput(itemId, -1_000m)]),
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Đánh dấu hoàn tất hợp lệ (đủ giá từng item) phải cập nhật trạng thái, thời gian, người xử lý,
    /// và nhập kho (uỷ quyền StockImportCommand) cho ĐÚNG từng item với tên/đơn vị/số lượng/giá khớp yêu cầu,
    /// Danh mục luôn là "Vật tư chính" (đặt riêng cho bệnh nhân theo option dịch vụ).</summary>
    [Test]
    public async Task HandleAsync_ValidRequestWithAllPrices_MarksAsDoneAndImportsEachItemAsMainCategory()
    {
        var request = MaterialRequest.Create("Niềng răng", "Bệnh nhân A", "BS X",
            [("Khay niềng", 1, "Cái"), ("Chỉ khâu 4/0", 2, "Cuộn")]);
        _db.MaterialRequests.Add(request);
        await _db.SaveChangesAsync();
        var khayNiengId = request.Items.First(i => i.ItemName == "Khay niềng").Id;
        var chiKhauId = request.Items.First(i => i.ItemName == "Chỉ khâu 4/0").Id;

        await _handler.Handle(
            new MarkMaterialRequestDoneCommand(request.Id, "staff1", [
                new MaterialRequestItemPriceInput(khayNiengId, 200_000m),
                new MaterialRequestItemPriceInput(chiKhauId, 15_000m),
            ]),
            CancellationToken.None);

        var updated = await _db.MaterialRequests.Include(r => r.Items).SingleAsync(r => r.Id == request.Id);
        updated.Status.Should().Be(MaterialRequestStatus.Done);
        updated.HandledBy.Should().Be("staff1");
        updated.HandledAt.Should().NotBeNull();
        updated.Items.Should().OnlyContain(i => i.SupplyTransactionId != null);

        await _sender.Received(1).Send(
            Arg.Is<StockImportCommand>(c => c.Name == "Khay niềng" && c.Unit == "Cái" && c.Quantity == 1
                && c.UnitPrice == 200_000m && c.Category == InventoryConstants.CategoryMain && c.CreatedBy == "staff1"),
            Arg.Any<CancellationToken>());
        await _sender.Received(1).Send(
            Arg.Is<StockImportCommand>(c => c.Name == "Chỉ khâu 4/0" && c.Unit == "Cuộn" && c.Quantity == 2
                && c.UnitPrice == 15_000m && c.Category == InventoryConstants.CategoryMain && c.CreatedBy == "staff1"),
            Arg.Any<CancellationToken>());
    }

    /// <summary>Đánh dấu hoàn tất một yêu cầu ĐÃ hoàn tất trước đó phải bị từ chối — tránh nhập kho trùng
    /// (double-import) nếu staff bấm lại nút xử lý.</summary>
    [Test]
    public async Task HandleAsync_AlreadyDoneRequest_ThrowsValidationExceptionAndDoesNotReimport()
    {
        var request = MaterialRequest.Create("Niềng răng", "Bệnh nhân A", "BS X", [("Khay niềng", 1, "Cái")]);
        request.MarkDone("staff1");
        _db.MaterialRequests.Add(request);
        await _db.SaveChangesAsync();
        var itemId = request.Items.First().Id;

        Func<Task> act = () => _handler.Handle(
            new MarkMaterialRequestDoneCommand(request.Id, "staff2", [new MaterialRequestItemPriceInput(itemId, 10_000m)]),
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        await _sender.DidNotReceive().Send(Arg.Any<StockImportCommand>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Yêu cầu ở trạng thái Ordered (đã đặt hàng, chưa nhập kho) vẫn xử lý Done bình thường —
    /// bước "đặt hàng" là tuỳ chọn, có thể bỏ qua thẳng từ Pending hoặc đi qua Ordered trước.</summary>
    [Test]
    public async Task HandleAsync_OrderedRequest_MarksAsDoneNormally()
    {
        var request = MaterialRequest.Create("Niềng răng", "Bệnh nhân A", "BS X", [("Khay niềng", 1, "Cái")]);
        request.MarkOrdered("staff1", "Đặt lab ABC");
        _db.MaterialRequests.Add(request);
        await _db.SaveChangesAsync();
        var itemId = request.Items.First().Id;

        await _handler.Handle(
            new MarkMaterialRequestDoneCommand(request.Id, "staff2", [new MaterialRequestItemPriceInput(itemId, 10_000m)]),
            CancellationToken.None);

        var updated = await _db.MaterialRequests.SingleAsync(r => r.Id == request.Id);
        updated.Status.Should().Be(MaterialRequestStatus.Done);
    }

    /// <summary>Số lượng thực nhận khác số lượng đã xin (nhà cung cấp giao thiếu/thừa) phải nhập kho ĐÚNG
    /// số thực nhận, không phải số đã xin ban đầu — và lưu lại ActualQuantity trên item.</summary>
    [Test]
    public async Task HandleAsync_ActualQuantityDiffersFromRequested_ImportsActualQuantity()
    {
        var request = MaterialRequest.Create("Niềng răng", "Bệnh nhân A", "BS X", [("Khay niềng", 5, "Cái")]);
        _db.MaterialRequests.Add(request);
        await _db.SaveChangesAsync();
        var itemId = request.Items.First().Id;

        await _handler.Handle(
            new MarkMaterialRequestDoneCommand(request.Id, "staff1",
                [new MaterialRequestItemPriceInput(itemId, 10_000m, ActualQuantity: 3)]),
            CancellationToken.None);

        await _sender.Received(1).Send(
            Arg.Is<StockImportCommand>(c => c.Name == "Khay niềng" && c.Quantity == 3),
            Arg.Any<CancellationToken>());
        var updatedItem = await _db.MaterialRequestItems.SingleAsync(i => i.Id == itemId);
        updatedItem.ActualQuantity.Should().Be(3);
        updatedItem.Quantity.Should().Be(5);
    }

    /// <summary>Không truyền ActualQuantity thì mặc định dùng đúng số lượng đã xin ban đầu.</summary>
    [Test]
    public async Task HandleAsync_NoActualQuantityProvided_DefaultsToRequestedQuantity()
    {
        var request = MaterialRequest.Create("Niềng răng", "Bệnh nhân A", "BS X", [("Khay niềng", 4, "Cái")]);
        _db.MaterialRequests.Add(request);
        await _db.SaveChangesAsync();
        var itemId = request.Items.First().Id;

        await _handler.Handle(
            new MarkMaterialRequestDoneCommand(request.Id, "staff1", [new MaterialRequestItemPriceInput(itemId, 10_000m)]),
            CancellationToken.None);

        await _sender.Received(1).Send(
            Arg.Is<StockImportCommand>(c => c.Quantity == 4),
            Arg.Any<CancellationToken>());
    }
}
