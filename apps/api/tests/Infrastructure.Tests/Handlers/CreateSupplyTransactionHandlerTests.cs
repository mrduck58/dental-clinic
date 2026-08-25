using DentalClinic.API.Application.UseCases.Inventory;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Infrastructure.Persistence;
using DentalClinic.API.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Handlers;

[TestFixture]
public class CreateSupplyTransactionHandlerTests
{
    private AppDbContext _db = null!;
    private IActivityLogService _activityLogService = null!;
    private ICurrentUserService _currentUser = null!;
    private CreateSupplyTransactionHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _activityLogService = Substitute.For<IActivityLogService>();
        _currentUser = Substitute.For<ICurrentUserService>();
        _handler = new CreateSupplyTransactionHandler(
            new SupplyItemRepository(_db), new SupplyTransactionRepository(_db), new RoomRepository(_db), _activityLogService, _currentUser);
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    private async Task<SupplyItem> SeedItemAsync(int quantity = 50)
    {
        var item = SupplyItem.Create("VT100", "Khẩu trang y tế", "Vật tư tiêu hao", "Hộp", quantity, 5);
        _db.SupplyItems.Add(item);
        await _db.SaveChangesAsync();
        return item;
    }

    /// <summary>Số lượng giao dịch phải lớn hơn 0.</summary>
    [Test]
    public async Task HandleAsync_ZeroOrNegativeQuantity_ThrowsValidationException()
    {
        var item = await SeedItemAsync();

        Func<Task> act = () => _handler.Handle(
            new CreateSupplyTransactionCommand(item.Id, "import", 0, null, "staff1"), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Loại giao dịch phải là "import" hoặc "export".</summary>
    [Test]
    public async Task HandleAsync_InvalidType_ThrowsValidationException()
    {
        var item = await SeedItemAsync();

        Func<Task> act = () => _handler.Handle(
            new CreateSupplyTransactionCommand(item.Id, "invalid-type", 10, null, "staff1"), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Vật tư không tồn tại phải báo lỗi NotFoundException.</summary>
    [Test]
    public async Task HandleAsync_ItemNotFound_ThrowsNotFoundException()
    {
        Func<Task> act = () => _handler.Handle(
            new CreateSupplyTransactionCommand(Guid.NewGuid(), "import", 10, null, "staff1"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>Xuất kho với số lượng vượt quá tồn kho hiện tại phải bị từ chối.</summary>
    [Test]
    public async Task HandleAsync_ExportQuantityExceedsStock_ThrowsValidationException()
    {
        var item = await SeedItemAsync(quantity: 5);

        Func<Task> act = () => _handler.Handle(
            new CreateSupplyTransactionCommand(item.Id, "export", 10, null, "staff1"), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Nhập kho hợp lệ phải cộng thêm đúng số lượng vào tồn kho.</summary>
    [Test]
    public async Task HandleAsync_ValidImport_IncreasesStockQuantity()
    {
        var item = await SeedItemAsync(quantity: 50);

        await _handler.Handle(new CreateSupplyTransactionCommand(item.Id, "import", 20, "Nhập thêm", "staff1"), CancellationToken.None);

        (await _db.SupplyItems.SingleAsync(i => i.Id == item.Id)).Quantity.Should().Be(70);
    }

    /// <summary>Xuất kho hợp lệ phải trừ đúng số lượng khỏi tồn kho và ghi log hoạt động.</summary>
    [Test]
    public async Task HandleAsync_ValidExport_DecreasesStockAndLogsActivity()
    {
        var item = await SeedItemAsync(quantity: 50);

        await _handler.Handle(new CreateSupplyTransactionCommand(item.Id, "export", 20, null, "staff1"), CancellationToken.None);

        (await _db.SupplyItems.SingleAsync(i => i.Id == item.Id)).Quantity.Should().Be(30);
        await _activityLogService.Received(1).LogAsync(
            Arg.Any<Guid?>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Xuất kho đúng bằng toàn bộ số lượng tồn kho hiện tại (ranh giới) phải thành công và đưa tồn kho về 0.</summary>
    [Test]
    public async Task HandleAsync_ExportExactlyRemainingStock_SucceedsWithZeroQuantity()
    {
        var item = await SeedItemAsync(quantity: 5);

        await _handler.Handle(new CreateSupplyTransactionCommand(item.Id, "export", 5, null, "staff1"), CancellationToken.None);

        (await _db.SupplyItems.SingleAsync(i => i.Id == item.Id)).Quantity.Should().Be(0);
    }

    /// <summary>Xuất kho nhiều hơn tồn kho đúng 1 đơn vị (ranh giới) phải bị từ chối.</summary>
    [Test]
    public async Task HandleAsync_ExportOneMoreThanStock_ThrowsValidationException()
    {
        var item = await SeedItemAsync(quantity: 5);

        Func<Task> act = () => _handler.Handle(
            new CreateSupplyTransactionCommand(item.Id, "export", 6, null, "staff1"), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Số lượng âm phải bị từ chối.</summary>
    [Test]
    public async Task HandleAsync_NegativeQuantity_ThrowsValidationException()
    {
        var item = await SeedItemAsync();

        Func<Task> act = () => _handler.Handle(
            new CreateSupplyTransactionCommand(item.Id, "import", -5, null, "staff1"), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Loại giao dịch phân biệt hoa/thường — "Import" viết hoa không khớp "import" nên bị từ chối.</summary>
    [Test]
    public async Task HandleAsync_TypeWithDifferentCase_ThrowsValidationException()
    {
        var item = await SeedItemAsync();

        Func<Task> act = () => _handler.Handle(
            new CreateSupplyTransactionCommand(item.Id, "Import", 10, null, "staff1"), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    private async Task<Room> SeedRoomAsync(string code = "P01", string name = "Phòng khám 1")
    {
        var room = Room.Create(code, name, "1", "");
        _db.Rooms.Add(room);
        await _db.SaveChangesAsync();
        return room;
    }

    /// <summary>Xuất kho gắn đúng phòng nhận phải trừ kho bình thường và trả về đúng tên phòng trong DTO.</summary>
    [Test]
    public async Task HandleAsync_ExportWithRoom_DecreasesStockAndReturnsRoomName()
    {
        var item = await SeedItemAsync(quantity: 50);
        var room = await SeedRoomAsync();

        var dto = await _handler.Handle(
            new CreateSupplyTransactionCommand(item.Id, "export", 10, "Hết găng tay", "staff1", room.Id), CancellationToken.None);

        dto.RoomName.Should().Be(room.Name);
        (await _db.SupplyItems.SingleAsync(i => i.Id == item.Id)).Quantity.Should().Be(40);
        (await _db.SupplyTransactions.SingleAsync(t => t.Id == dto.Id)).RoomId.Should().Be(room.Id);
    }

    /// <summary>Nhập kho không được phép gắn phòng nhận — chỉ xuất kho mới có ý nghĩa "cấp cho phòng".</summary>
    [Test]
    public async Task HandleAsync_ImportWithRoom_ThrowsValidationException()
    {
        var item = await SeedItemAsync();
        var room = await SeedRoomAsync();

        Func<Task> act = () => _handler.Handle(
            new CreateSupplyTransactionCommand(item.Id, "import", 10, null, "staff1", room.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Phòng không tồn tại phải báo lỗi NotFoundException, không được trừ kho.</summary>
    [Test]
    public async Task HandleAsync_ExportWithNonExistentRoom_ThrowsNotFoundExceptionAndDoesNotAdjustStock()
    {
        var item = await SeedItemAsync(quantity: 50);

        Func<Task> act = () => _handler.Handle(
            new CreateSupplyTransactionCommand(item.Id, "export", 10, null, "staff1", Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        (await _db.SupplyItems.SingleAsync(i => i.Id == item.Id)).Quantity.Should().Be(50);
    }
}
