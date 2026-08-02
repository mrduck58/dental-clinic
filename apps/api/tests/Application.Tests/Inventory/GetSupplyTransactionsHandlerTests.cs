using DentalClinic.API.Application.UseCases.Inventory;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Inventory;

[TestFixture]
public class GetSupplyTransactionsHandlerTests
{
    private ISupplyTransactionRepository _repo = null!;
    private GetSupplyTransactionsHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = Substitute.For<ISupplyTransactionRepository>();
        _handler = new GetSupplyTransactionsHandler(_repo);
    }

    /// <summary>Không có giao dịch nào phải trả về danh sách rỗng.</summary>
    [Test]
    public async Task HandleAsync_NoTransactions_ReturnsEmptyList()
    {
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<SupplyTransaction>());

        var result = await _handler.Handle(new GetSupplyTransactionsQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    /// <summary>Giao dịch phải được ánh xạ đúng sang DTO, bao gồm tên vật tư từ navigation property.</summary>
    [Test]
    public async Task HandleAsync_HasTransactions_MapsToDataTransferObjectsCorrectly()
    {
        var item = SupplyItem.Create("VT001", "Bông gòn y tế", "Vật tư tiêu hao", "Gói", 50, 5);
        var tx = SupplyTransaction.Create(item.Id, "import", 20, "Nhập đầu tháng", "staff1");
        typeof(SupplyTransaction).GetProperty(nameof(SupplyTransaction.SupplyItem))!.SetValue(tx, item);
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<SupplyTransaction> { tx });

        var result = (await _handler.Handle(new GetSupplyTransactionsQuery(), CancellationToken.None)).ToList();

        result.Should().ContainSingle();
        result[0].ItemName.Should().Be("Bông gòn y tế");
        result[0].Quantity.Should().Be(20);
    }

    /// <summary>Nhiều giao dịch phải được ánh xạ đầy đủ sang DTO, giữ đúng số lượng bản ghi.</summary>
    [Test]
    public async Task HandleAsync_MultipleTransactions_ReturnsAllMappedToDtos()
    {
        var item1 = SupplyItem.Create("VT001", "Bông gòn y tế", "Vật tư tiêu hao", "Gói", 50, 5);
        var item2 = SupplyItem.Create("VT002", "Găng tay y tế", "Vật tư tiêu hao", "Hộp", 100, 10);
        var tx1 = SupplyTransaction.Create(item1.Id, "import", 20, "Nhập đầu tháng", "staff1");
        var tx2 = SupplyTransaction.Create(item2.Id, "export", 5, null, "staff2");
        typeof(SupplyTransaction).GetProperty(nameof(SupplyTransaction.SupplyItem))!.SetValue(tx1, item1);
        typeof(SupplyTransaction).GetProperty(nameof(SupplyTransaction.SupplyItem))!.SetValue(tx2, item2);
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<SupplyTransaction> { tx1, tx2 });

        var result = (await _handler.Handle(new GetSupplyTransactionsQuery(), CancellationToken.None)).ToList();

        result.Should().HaveCount(2);
        result.Should().Contain(t => t.ItemName == "Bông gòn y tế" && t.Type == "import");
        result.Should().Contain(t => t.ItemName == "Găng tay y tế" && t.Type == "export" && t.Note == null);
    }

    /// <summary>Mọi trường của giao dịch (Id, SupplyItemId, Note, CreatedBy) phải được ánh xạ chính xác sang DTO.</summary>
    [Test]
    public async Task HandleAsync_TransactionFields_MapsAllPropertiesCorrectly()
    {
        var item = SupplyItem.Create("VT001", "Bông gòn y tế", "Vật tư tiêu hao", "Gói", 50, 5);
        var tx = SupplyTransaction.Create(item.Id, "export", 15, "Xuất cho phòng khám 2", "staff9");
        typeof(SupplyTransaction).GetProperty(nameof(SupplyTransaction.SupplyItem))!.SetValue(tx, item);
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<SupplyTransaction> { tx });

        var result = (await _handler.Handle(new GetSupplyTransactionsQuery(), CancellationToken.None)).ToList();

        result[0].Id.Should().Be(tx.Id);
        result[0].SupplyItemId.Should().Be(item.Id);
        result[0].Type.Should().Be("export");
        result[0].Quantity.Should().Be(15);
        result[0].Note.Should().Be("Xuất cho phòng khám 2");
        result[0].CreatedBy.Should().Be("staff9");
    }
}
