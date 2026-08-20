using DentalClinic.API.Application.UseCases.Inventory;
using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Infrastructure.Persistence;
using DentalClinic.API.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Handlers;

[TestFixture]
public class UpdateSupplyItemHandlerTests
{
    private AppDbContext _db = null!;
    private UpdateSupplyItemHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _handler = new UpdateSupplyItemHandler(new SupplyItemRepository(_db));
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    private async Task<SupplyItem> SeedItemAsync()
    {
        var item = SupplyItem.Create("VT001", "Găng tay y tế", InventoryConstants.CategoryConsumable, "Hộp", 100, 10, price: 20_000m);
        _db.SupplyItems.Add(item);
        await _db.SaveChangesAsync();
        return item;
    }

    [Test]
    public async Task HandleAsync_ItemNotFound_ThrowsNotFoundException()
    {
        Func<Task> act = () => _handler.Handle(
            new UpdateSupplyItemCommand(Guid.NewGuid(), "Tên", InventoryConstants.CategoryConsumable, "Hộp", 5, null),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task HandleAsync_CategoryNotInAllowedList_ThrowsValidationException()
    {
        var item = await SeedItemAsync();

        Func<Task> act = () => _handler.Handle(
            new UpdateSupplyItemCommand(item.Id, item.Name, "Bảo hộ", item.Unit, item.MinQuantity, null),
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task HandleAsync_InvalidUnit_ThrowsValidationException()
    {
        var item = await SeedItemAsync();

        Func<Task> act = () => _handler.Handle(
            new UpdateSupplyItemCommand(item.Id, item.Name, item.Category, "Kilogram", item.MinQuantity, null),
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Đổi Danh mục từ nhóm dùng chung sang "Vật tư chính" phải tự cập nhật lại OrderType thành "custom".</summary>
    [Test]
    public async Task HandleAsync_ChangeCategoryToMain_UpdatesOrderTypeToCustom()
    {
        var item = await SeedItemAsync();
        item.OrderType.Should().Be("standard");

        var result = await _handler.Handle(
            new UpdateSupplyItemCommand(item.Id, item.Name, InventoryConstants.CategoryMain, item.Unit, item.MinQuantity, null),
            CancellationToken.None);

        result.OrderType.Should().Be("custom");
        (await _db.SupplyItems.SingleAsync()).OrderType.Should().Be("custom");
    }

    [Test]
    public async Task HandleAsync_ValidRequest_UpdatesNameUnitMinQuantityAndPrice()
    {
        var item = await SeedItemAsync();

        var result = await _handler.Handle(
            new UpdateSupplyItemCommand(item.Id, "Găng tay y tế (M)", InventoryConstants.CategoryConsumable, "Cái", 20, 99_000m),
            CancellationToken.None);

        result.Name.Should().Be("Găng tay y tế (M)");
        result.Unit.Should().Be("Cái");
        result.MinQuantity.Should().Be(20);
        result.Price.Should().Be(99_000m);
    }

    /// <summary>Không truyền giá (null) phải giữ nguyên giá cũ, không xóa về null.</summary>
    [Test]
    public async Task HandleAsync_NullPrice_KeepsExistingPrice()
    {
        var item = await SeedItemAsync();

        var result = await _handler.Handle(
            new UpdateSupplyItemCommand(item.Id, item.Name, item.Category, item.Unit, item.MinQuantity, null),
            CancellationToken.None);

        result.Price.Should().Be(20_000m);
    }
}
