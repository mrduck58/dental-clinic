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
public class DeleteSupplyItemHandlerTests
{
    private AppDbContext _db = null!;
    private DeleteSupplyItemHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _handler = new DeleteSupplyItemHandler(new SupplyItemRepository(_db));
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    [Test]
    public async Task HandleAsync_ItemNotFound_ThrowsNotFoundException()
    {
        Func<Task> act = () => _handler.Handle(new DeleteSupplyItemCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task HandleAsync_ItemWithNoTransactions_DeletesSuccessfully()
    {
        var item = SupplyItem.Create("VT001", "Vật tư chưa dùng", InventoryConstants.CategoryConsumable, "Cái", 10, 5);
        _db.SupplyItems.Add(item);
        await _db.SaveChangesAsync();

        await _handler.Handle(new DeleteSupplyItemCommand(item.Id), CancellationToken.None);

        (await _db.SupplyItems.CountAsync()).Should().Be(0);
    }

    /// <summary>Vật tư đã có giao dịch (nhập/xuất) phải bị chặn xóa với thông báo rõ ràng, không lộ lỗi DB.</summary>
    [Test]
    public async Task HandleAsync_ItemWithTransactionHistory_ThrowsValidationException()
    {
        var item = SupplyItem.Create("VT002", "Vật tư đã dùng", InventoryConstants.CategoryConsumable, "Cái", 10, 5);
        _db.SupplyItems.Add(item);
        _db.SupplyTransactions.Add(SupplyTransaction.Create(item.Id, "import", 10, null, "staff1"));
        await _db.SaveChangesAsync();

        Func<Task> act = () => _handler.Handle(new DeleteSupplyItemCommand(item.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        (await _db.SupplyItems.CountAsync()).Should().Be(1);
    }
}
