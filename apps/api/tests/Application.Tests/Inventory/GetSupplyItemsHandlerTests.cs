using DentalClinic.API.Application.UseCases.Inventory;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Inventory;

[TestFixture]
public class GetSupplyItemsHandlerTests
{
    private ISupplyItemRepository _repo = null!;
    private GetSupplyItemsHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = Substitute.For<ISupplyItemRepository>();
        _handler = new GetSupplyItemsHandler(_repo);
    }

    private static List<SupplyItem> SeedItems() =>
    [
        SupplyItem.Create("VT001", "Găng tay y tế", "Vật tư tiêu hao", "Hộp", 100, 10),
        SupplyItem.Create("VT002", "Kim tiêm", "Vật tư tiêu hao", "Cái", 5, 20), // dưới mức tối thiểu
        SupplyItem.Create("VT003", "Ghế nha khoa", "Thiết bị", "Cái", 3, 1),
    ];

    /// <summary>Không truyền bộ lọc phải trả về toàn bộ vật tư.</summary>
    [Test]
    public async Task HandleAsync_NoFilters_ReturnsAllItems()
    {
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(SeedItems());

        var result = (await _handler.HandleAsync(null, null)).ToList();

        result.Should().HaveCount(3);
    }

    /// <summary>Tìm theo tên hoặc mã (không phân biệt hoa/thường) phải lọc đúng kết quả.</summary>
    [Test]
    public async Task HandleAsync_SearchByName_ReturnsMatchingItemsOnly()
    {
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(SeedItems());

        var result = (await _handler.HandleAsync("găng tay", null)).ToList();

        result.Should().ContainSingle(i => i.Code == "VT001");
    }

    /// <summary>Lọc theo danh mục cụ thể phải loại bỏ các vật tư thuộc danh mục khác.</summary>
    [Test]
    public async Task HandleAsync_FilterByCategory_ReturnsOnlyThatCategory()
    {
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(SeedItems());

        var result = (await _handler.HandleAsync(null, "Thiết bị")).ToList();

        result.Should().ContainSingle(i => i.Code == "VT003");
    }

    /// <summary>Danh mục "Tất cả" phải được coi như không lọc, trả về mọi vật tư.</summary>
    [Test]
    public async Task HandleAsync_CategoryAllOption_ReturnsAllItems()
    {
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(SeedItems());

        var result = (await _handler.HandleAsync(null, "Tất cả")).ToList();

        result.Should().HaveCount(3);
    }

    /// <summary>Vật tư có số lượng bằng hoặc dưới mức tối thiểu phải được đánh dấu IsLow = true.</summary>
    [Test]
    public async Task HandleAsync_ItemBelowMinQuantity_MarkedAsLow()
    {
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(SeedItems());

        var result = (await _handler.HandleAsync(null, null)).ToList();

        result.Single(i => i.Code == "VT002").IsLow.Should().BeTrue();
        result.Single(i => i.Code == "VT001").IsLow.Should().BeFalse();
    }

    /// <summary>Vật tư có số lượng đúng bằng mức tối thiểu (ranh giới) vẫn phải được đánh dấu IsLow = true.</summary>
    [Test]
    public async Task HandleAsync_QuantityExactlyAtMinQuantity_MarkedAsLow()
    {
        var items = new List<SupplyItem> { SupplyItem.Create("VT010", "Chỉ khâu", "Vật tư tiêu hao", "Cuộn", 10, 10) };
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(items);

        var result = (await _handler.HandleAsync(null, null)).ToList();

        result.Single().IsLow.Should().BeTrue();
    }

    /// <summary>Tìm theo mã vật tư (không phân biệt hoa/thường) phải trả về đúng kết quả.</summary>
    [Test]
    public async Task HandleAsync_SearchByCode_ReturnsMatchingItemsOnly()
    {
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(SeedItems());

        var result = (await _handler.HandleAsync("vt003", null)).ToList();

        result.Should().ContainSingle(i => i.Code == "VT003");
    }

    /// <summary>Từ khóa tìm kiếm không khớp với bất kỳ vật tư nào phải trả về danh sách rỗng.</summary>
    [Test]
    public async Task HandleAsync_SearchNoMatches_ReturnsEmptyList()
    {
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(SeedItems());

        var result = (await _handler.HandleAsync("không tồn tại", null)).ToList();

        result.Should().BeEmpty();
    }

    /// <summary>Kết hợp đồng thời tìm kiếm và lọc theo danh mục phải trả về đúng giao của hai điều kiện.</summary>
    [Test]
    public async Task HandleAsync_SearchAndCategoryCombined_ReturnsIntersectionOnly()
    {
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(SeedItems());

        var result = (await _handler.HandleAsync("kim tiêm", "Vật tư tiêu hao")).ToList();

        result.Should().ContainSingle(i => i.Code == "VT002");
    }

    /// <summary>Kết hợp tìm kiếm và danh mục mà không có vật tư nào thỏa cả hai điều kiện phải trả về rỗng.</summary>
    [Test]
    public async Task HandleAsync_SearchAndCategoryCombined_NoIntersection_ReturnsEmptyList()
    {
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(SeedItems());

        var result = (await _handler.HandleAsync("kim tiêm", "Thiết bị")).ToList();

        result.Should().BeEmpty();
    }

    /// <summary>Lọc theo danh mục hiện đang so khớp chuỗi tuyệt đối (phân biệt hoa/thường),
    /// nên danh mục khác hoa/thường so với dữ liệu lưu trữ sẽ không khớp.</summary>
    [Test]
    public async Task HandleAsync_CategoryFilterDifferentCase_DoesNotMatch()
    {
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(SeedItems());

        var result = (await _handler.HandleAsync(null, "thiết bị")).ToList();

        result.Should().BeEmpty();
    }
}
