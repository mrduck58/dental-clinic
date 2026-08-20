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
public class CreateSupplyItemHandlerTests
{
    private AppDbContext _db = null!;
    private CreateSupplyItemHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _handler = new CreateSupplyItemHandler(new SupplyItemRepository(_db));
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    private static CreateSupplyItemCommand MakeRequest(string code = "VT001") =>
        new(code, "Găng tay y tế", "Vật tư tiêu hao", "Hộp", 100, 10);

    /// <summary>Tên vật tư để trống phải bị từ chối.</summary>
    [Test]
    public async Task HandleAsync_EmptyName_ThrowsValidationException()
    {
        Func<Task> act = () => _handler.Handle(MakeRequest() with { Name = " " }, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Mã vật tư để trống phải bị từ chối.</summary>
    [Test]
    public async Task HandleAsync_EmptyCode_ThrowsValidationException()
    {
        Func<Task> act = () => _handler.Handle(MakeRequest() with { Code = " " }, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Mã vật tư đã tồn tại (không phân biệt hoa/thường) phải bị từ chối để tránh trùng lặp.</summary>
    [Test]
    public async Task HandleAsync_DuplicateCode_ThrowsConflictException()
    {
        await _handler.Handle(MakeRequest("VT001"), CancellationToken.None);

        Func<Task> act = () => _handler.Handle(MakeRequest("vt001"), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    /// <summary>Tạo vật tư hợp lệ phải lưu vào DB với mã được chuẩn hóa in hoa.</summary>
    [Test]
    public async Task HandleAsync_ValidRequest_SavesItemWithUppercaseCode()
    {
        var result = await _handler.Handle(MakeRequest("vt002"), CancellationToken.None);

        result.Code.Should().Be("VT002");
        (await _db.SupplyItems.CountAsync()).Should().Be(1);
    }

    /// <summary>Mã vật tư có khoảng trắng thừa phải được cắt bỏ và chuyển thành in hoa khi lưu.</summary>
    [Test]
    public async Task HandleAsync_CodeWithWhitespace_TrimsAndUppercasesCode()
    {
        var result = await _handler.Handle(MakeRequest("  vt010  "), CancellationToken.None);

        result.Code.Should().Be("VT010");
    }

    /// <summary>Mã vật tư trùng chỉ khác nhau về khoảng trắng và hoa/thường vẫn phải bị từ chối.</summary>
    [Test]
    public async Task HandleAsync_DuplicateCodeDifferingByWhitespaceAndCase_ThrowsConflictException()
    {
        await _handler.Handle(MakeRequest("VT001"), CancellationToken.None);

        Func<Task> act = () => _handler.Handle(MakeRequest("  vt001  "), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    /// <summary>Tên vật tư có khoảng trắng thừa phải được cắt bỏ khi lưu.</summary>
    [Test]
    public async Task HandleAsync_NameWithWhitespace_TrimsName()
    {
        var result = await _handler.Handle(MakeRequest("VT020") with { Name = "  Chỉ nha khoa  " }, CancellationToken.None);

        result.Name.Should().Be("Chỉ nha khoa");
    }

    /// <summary>Danh mục không nằm trong 3 danh mục cho phép phải bị từ chối.</summary>
    [Test]
    public async Task HandleAsync_CategoryNotInAllowedList_ThrowsValidationException()
    {
        Func<Task> act = () => _handler.Handle(MakeRequest() with { Category = "Bảo hộ" }, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Tạo vật tư danh mục "Vật tư chính" phải tự suy ra OrderType "custom" — không cho chọn tay.</summary>
    [Test]
    public async Task HandleAsync_CategoryMain_DerivesCustomOrderType()
    {
        var result = await _handler.Handle(MakeRequest("VT030") with { Category = InventoryConstants.CategoryMain }, CancellationToken.None);

        result.OrderType.Should().Be("custom");
    }
}
