using DentalClinic.API.Application.UseCases.Inventory;
using DentalClinic.API.Domain.Constants;
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
public class StockImportHandlerTests
{
    private AppDbContext _db = null!;
    private IActivityLogService _activityLogService = null!;
    private ICurrentUserService _currentUser = null!;
    private StockImportHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _activityLogService = Substitute.For<IActivityLogService>();
        _currentUser = Substitute.For<ICurrentUserService>();
        _handler = new StockImportHandler(
            new SupplyItemRepository(_db), new SupplyTransactionRepository(_db), _activityLogService, _currentUser);
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    private static StockImportCommand MakeRequest(string name = "Chỉ nha khoa") =>
        new(name, "Cuộn", InventoryConstants.CategoryConsumable, 30, null, 50_000m, "staff1");

    /// <summary>Tên vật tư để trống phải bị từ chối.</summary>
    [Test]
    public async Task HandleAsync_EmptyName_ThrowsValidationException()
    {
        Func<Task> act = () => _handler.Handle(MakeRequest() with { Name = " " }, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Đơn vị không nằm trong danh sách cho phép phải bị từ chối.</summary>
    [Test]
    public async Task HandleAsync_InvalidUnit_ThrowsValidationException()
    {
        Func<Task> act = () => _handler.Handle(MakeRequest() with { Unit = "Kilogram" }, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Số lượng phải lớn hơn 0.</summary>
    [Test]
    public async Task HandleAsync_ZeroQuantity_ThrowsValidationException()
    {
        Func<Task> act = () => _handler.Handle(MakeRequest() with { Quantity = 0 }, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Bỏ trống đơn giá phải bị từ chối — nhập kho giờ bắt buộc phải có giá, tránh lọt khỏi báo
    /// cáo chi phí vật tư mà không ai biết (xem ExpenseQueryService.GetSummaryAsync).</summary>
    [Test]
    public async Task HandleAsync_NullUnitPrice_ThrowsValidationException()
    {
        Func<Task> act = () => _handler.Handle(MakeRequest() with { UnitPrice = null }, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        (await _db.SupplyTransactions.CountAsync()).Should().Be(0);
    }

    /// <summary>Đơn giá bằng 0 vẫn phải được chấp nhận (vd hàng biếu/mẫu miễn phí) — chỉ bắt buộc PHẢI
    /// nhập, không bắt buộc phải lớn hơn 0.</summary>
    [Test]
    public async Task HandleAsync_ZeroUnitPrice_IsAccepted()
    {
        var result = await _handler.Handle(MakeRequest() with { UnitPrice = 0m }, CancellationToken.None);

        result.UnitPrice.Should().Be(0m);
    }

    /// <summary>Vật tư chưa tồn tại (theo tên) phải được tạo mới với mã tự sinh.</summary>
    [Test]
    public async Task HandleAsync_NewItemName_CreatesNewSupplyItem()
    {
        var result = await _handler.Handle(MakeRequest("Vật tư hoàn toàn mới"), CancellationToken.None);

        result.ItemName.Should().Be("Vật tư hoàn toàn mới");
        (await _db.SupplyItems.CountAsync()).Should().Be(1);
    }

    /// <summary>Vật tư đã tồn tại (trùng tên, không phân biệt hoa/thường) phải cộng dồn số lượng,
    /// giữ nguyên đơn vị cũ thay vì tạo bản ghi trùng.</summary>
    [Test]
    public async Task HandleAsync_ExistingItemName_IncreasesQuantityInsteadOfDuplicating()
    {
        var existing = SupplyItem.Create("VT999", "chỉ nha khoa", "Vật tư tiêu hao", "Cuộn", 10, 5);
        _db.SupplyItems.Add(existing);
        await _db.SaveChangesAsync();

        await _handler.Handle(MakeRequest("Chỉ Nha Khoa"), CancellationToken.None);

        (await _db.SupplyItems.CountAsync()).Should().Be(1);
        (await _db.SupplyItems.SingleAsync()).Quantity.Should().Be(40);
    }

    /// <summary>Nhập kho hợp lệ phải tạo giao dịch loại "import" và ghi log hoạt động.</summary>
    [Test]
    public async Task HandleAsync_ValidRequest_CreatesImportTransactionAndLogsActivity()
    {
        var result = await _handler.Handle(MakeRequest(), CancellationToken.None);

        result.Type.Should().Be("import");
        (await _db.SupplyTransactions.CountAsync()).Should().Be(1);
        await _activityLogService.Received(1).LogAsync(
            Arg.Any<Guid?>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Mỗi đơn vị nằm trong danh sách cho phép (AllowedUnits) phải được chấp nhận.</summary>
    [TestCase("Cái")]
    [TestCase("Hộp")]
    [TestCase("Tuýp")]
    [TestCase("Cuộn")]
    [TestCase("Chai")]
    [TestCase("Gói")]
    [TestCase("Bộ")]
    public async Task HandleAsync_EachAllowedUnit_IsAccepted(string unit)
    {
        var result = await _handler.Handle(MakeRequest($"Vật tư {unit}") with { Unit = unit }, CancellationToken.None);

        result.Should().NotBeNull();
        (await _db.SupplyItems.SingleAsync()).Unit.Should().Be(unit);
    }

    /// <summary>Danh mục để trống phải bị từ chối.</summary>
    [Test]
    public async Task HandleAsync_EmptyCategory_ThrowsValidationException()
    {
        Func<Task> act = () => _handler.Handle(MakeRequest() with { Category = " " }, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Danh mục không nằm trong 3 danh mục cho phép (Vật tư chính/Tiêu hao/Kỹ thuật-labo) phải bị từ chối.</summary>
    [Test]
    public async Task HandleAsync_CategoryNotInAllowedList_ThrowsValidationException()
    {
        Func<Task> act = () => _handler.Handle(MakeRequest() with { Category = "Bảo hộ" }, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Vật tư mới tạo với danh mục "Vật tư chính" phải tự suy ra OrderType "custom".</summary>
    [Test]
    public async Task HandleAsync_CategoryMain_DerivesCustomOrderType()
    {
        await _handler.Handle(MakeRequest("Mão Titan") with { Category = InventoryConstants.CategoryMain }, CancellationToken.None);

        (await _db.SupplyItems.SingleAsync()).OrderType.Should().Be("custom");
    }

    /// <summary>Vật tư mới tạo với danh mục Tiêu hao/Kỹ thuật-labo phải tự suy ra OrderType "standard".</summary>
    [TestCase(InventoryConstants.CategoryConsumable)]
    [TestCase(InventoryConstants.CategoryTechnical)]
    public async Task HandleAsync_NonMainCategory_DerivesStandardOrderType(string category)
    {
        await _handler.Handle(MakeRequest("Vật tư dùng chung") with { Category = category }, CancellationToken.None);

        (await _db.SupplyItems.SingleAsync()).OrderType.Should().Be("standard");
    }

    /// <summary>Số lượng âm phải bị từ chối.</summary>
    [Test]
    public async Task HandleAsync_NegativeQuantity_ThrowsValidationException()
    {
        Func<Task> act = () => _handler.Handle(MakeRequest() with { Quantity = -5 }, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Tên vật tư có khoảng trắng thừa vẫn phải khớp với vật tư đã tồn tại để cộng dồn số lượng
    /// thay vì tạo bản ghi mới.</summary>
    [Test]
    public async Task HandleAsync_ExistingItemNameWithWhitespace_MatchesAndIncreasesQuantity()
    {
        var existing = SupplyItem.Create("VT999", "Chỉ nha khoa", "Vật tư tiêu hao", "Cuộn", 10, 5);
        _db.SupplyItems.Add(existing);
        await _db.SaveChangesAsync();

        await _handler.Handle(MakeRequest("  Chỉ nha khoa  "), CancellationToken.None);

        (await _db.SupplyItems.CountAsync()).Should().Be(1);
        (await _db.SupplyItems.SingleAsync()).Quantity.Should().Be(40);
    }

    /// <summary>Khi cộng dồn vào vật tư đã tồn tại, đơn vị của yêu cầu mới phải bị bỏ qua — giữ nguyên đơn vị cũ.</summary>
    [Test]
    public async Task HandleAsync_ExistingItemWithDifferentRequestUnit_KeepsOriginalUnit()
    {
        var existing = SupplyItem.Create("VT998", "Băng gạc", "Vật tư tiêu hao", "Gói", 10, 5);
        _db.SupplyItems.Add(existing);
        await _db.SaveChangesAsync();

        await _handler.Handle(MakeRequest("Băng gạc") with { Unit = "Hộp" }, CancellationToken.None);

        (await _db.SupplyItems.SingleAsync()).Unit.Should().Be("Gói");
    }

    /// <summary>Vật tư "standard" đã tồn tại: cộng dồn số lượng phải đi kèm cập nhật giá tham chiếu
    /// theo lần nhập mới nhất (giá nhà cung cấp trôi nổi theo thời gian).</summary>
    [Test]
    public async Task HandleAsync_ExistingStandardItem_UpdatesReferencePriceToLatestImport()
    {
        var existing = SupplyItem.Create("VT997", "Găng tay", InventoryConstants.CategoryConsumable, "Hộp", 10, 5, price: 10_000m);
        _db.SupplyItems.Add(existing);
        await _db.SaveChangesAsync();

        await _handler.Handle(MakeRequest("Găng tay") with { UnitPrice = 50_000m }, CancellationToken.None);

        (await _db.SupplyItems.SingleAsync()).Price.Should().Be(50_000m);
    }

    /// <summary>Vật tư "custom" (đặt riêng cho bệnh nhân) đã tồn tại: cộng dồn số lượng như bình thường
    /// nhưng KHÔNG được ghi đè giá tham chiếu — mỗi lần nhập là 1 ca khác nhau, giá khác nhau là bình thường;
    /// giá thật của từng lần nhập vẫn tra đúng qua SupplyTransaction (Lịch sử giao dịch), không mất dữ liệu.</summary>
    [Test]
    public async Task HandleAsync_ExistingCustomItem_KeepsOriginalReferencePrice()
    {
        var existing = SupplyItem.Create("VT996", "Răng sứ Cercon", InventoryConstants.CategoryMain, "Cái", 1, 0, price: 10_000m);
        _db.SupplyItems.Add(existing);
        await _db.SaveChangesAsync();

        await _handler.Handle(MakeRequest("Răng sứ Cercon") with { UnitPrice = 50_000m }, CancellationToken.None);

        var item = await _db.SupplyItems.SingleAsync();
        item.Price.Should().Be(10_000m);
        item.Quantity.Should().Be(31); // vẫn cộng dồn số lượng bình thường (1 + 30 từ MakeRequest)

        var tx = await _db.SupplyTransactions.SingleAsync();
        tx.UnitPrice.Should().Be(50_000m); // giá thật của lần nhập này vẫn được lưu đúng ở giao dịch
    }
}
