using DentalClinic.API.Application.DTOs.Inventory;
using DentalClinic.API.Application.UseCases.Inventory;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Infrastructure.Persistence;
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
        _handler = new StockImportHandler(_db, _activityLogService, _currentUser);
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    private static StockImportRequest MakeRequest(string name = "Chỉ nha khoa") =>
        new(name, "Cuộn", "Vật tư tiêu hao", 30, null);

    /// <summary>Tên vật tư để trống phải bị từ chối.</summary>
    [Test]
    public async Task HandleAsync_EmptyName_ThrowsValidationException()
    {
        Func<Task> act = () => _handler.HandleAsync(MakeRequest() with { Name = " " }, "staff1");

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Đơn vị không nằm trong danh sách cho phép phải bị từ chối.</summary>
    [Test]
    public async Task HandleAsync_InvalidUnit_ThrowsValidationException()
    {
        Func<Task> act = () => _handler.HandleAsync(MakeRequest() with { Unit = "Kilogram" }, "staff1");

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Số lượng phải lớn hơn 0.</summary>
    [Test]
    public async Task HandleAsync_ZeroQuantity_ThrowsValidationException()
    {
        Func<Task> act = () => _handler.HandleAsync(MakeRequest() with { Quantity = 0 }, "staff1");

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Vật tư chưa tồn tại (theo tên) phải được tạo mới với mã tự sinh.</summary>
    [Test]
    public async Task HandleAsync_NewItemName_CreatesNewSupplyItem()
    {
        var result = await _handler.HandleAsync(MakeRequest("Vật tư hoàn toàn mới"), "staff1");

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

        await _handler.HandleAsync(MakeRequest("Chỉ Nha Khoa"), "staff1");

        (await _db.SupplyItems.CountAsync()).Should().Be(1);
        (await _db.SupplyItems.SingleAsync()).Quantity.Should().Be(40);
    }

    /// <summary>Nhập kho hợp lệ phải tạo giao dịch loại "import" và ghi log hoạt động.</summary>
    [Test]
    public async Task HandleAsync_ValidRequest_CreatesImportTransactionAndLogsActivity()
    {
        var result = await _handler.HandleAsync(MakeRequest(), "staff1");

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
        var result = await _handler.HandleAsync(MakeRequest($"Vật tư {unit}") with { Unit = unit }, "staff1");

        result.Should().NotBeNull();
        (await _db.SupplyItems.SingleAsync()).Unit.Should().Be(unit);
    }

    /// <summary>Danh mục để trống phải bị từ chối.</summary>
    [Test]
    public async Task HandleAsync_EmptyCategory_ThrowsValidationException()
    {
        Func<Task> act = () => _handler.HandleAsync(MakeRequest() with { Category = " " }, "staff1");

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Số lượng âm phải bị từ chối.</summary>
    [Test]
    public async Task HandleAsync_NegativeQuantity_ThrowsValidationException()
    {
        Func<Task> act = () => _handler.HandleAsync(MakeRequest() with { Quantity = -5 }, "staff1");

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

        await _handler.HandleAsync(MakeRequest("  Chỉ nha khoa  "), "staff1");

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

        await _handler.HandleAsync(MakeRequest("Băng gạc") with { Unit = "Hộp" }, "staff1");

        (await _db.SupplyItems.SingleAsync()).Unit.Should().Be("Gói");
    }
}
