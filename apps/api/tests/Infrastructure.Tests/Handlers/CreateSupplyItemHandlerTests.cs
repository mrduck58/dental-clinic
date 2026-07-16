using DentalClinic.API.Application.DTOs.Inventory;
using DentalClinic.API.Application.UseCases.Inventory;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Infrastructure.Persistence;
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
        _handler = new CreateSupplyItemHandler(_db);
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    private static CreateSupplyItemRequest MakeRequest(string code = "VT001") =>
        new(code, "Găng tay y tế", "Vật tư tiêu hao", "Hộp", 100, 10);

    /// <summary>Tên vật tư để trống phải bị từ chối.</summary>
    [Test]
    public async Task HandleAsync_EmptyName_ThrowsValidationException()
    {
        Func<Task> act = () => _handler.HandleAsync(MakeRequest() with { Name = " " });

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Mã vật tư để trống phải bị từ chối.</summary>
    [Test]
    public async Task HandleAsync_EmptyCode_ThrowsValidationException()
    {
        Func<Task> act = () => _handler.HandleAsync(MakeRequest() with { Code = " " });

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Mã vật tư đã tồn tại (không phân biệt hoa/thường) phải bị từ chối để tránh trùng lặp.</summary>
    [Test]
    public async Task HandleAsync_DuplicateCode_ThrowsConflictException()
    {
        await _handler.HandleAsync(MakeRequest("VT001"));

        Func<Task> act = () => _handler.HandleAsync(MakeRequest("vt001"));

        await act.Should().ThrowAsync<ConflictException>();
    }

    /// <summary>Tạo vật tư hợp lệ phải lưu vào DB với mã được chuẩn hóa in hoa.</summary>
    [Test]
    public async Task HandleAsync_ValidRequest_SavesItemWithUppercaseCode()
    {
        var result = await _handler.HandleAsync(MakeRequest("vt002"));

        result.Code.Should().Be("VT002");
        (await _db.SupplyItems.CountAsync()).Should().Be(1);
    }

    /// <summary>Mã vật tư có khoảng trắng thừa phải được cắt bỏ và chuyển thành in hoa khi lưu.</summary>
    [Test]
    public async Task HandleAsync_CodeWithWhitespace_TrimsAndUppercasesCode()
    {
        var result = await _handler.HandleAsync(MakeRequest("  vt010  "));

        result.Code.Should().Be("VT010");
    }

    /// <summary>Mã vật tư trùng chỉ khác nhau về khoảng trắng và hoa/thường vẫn phải bị từ chối.</summary>
    [Test]
    public async Task HandleAsync_DuplicateCodeDifferingByWhitespaceAndCase_ThrowsConflictException()
    {
        await _handler.HandleAsync(MakeRequest("VT001"));

        Func<Task> act = () => _handler.HandleAsync(MakeRequest("  vt001  "));

        await act.Should().ThrowAsync<ConflictException>();
    }

    /// <summary>Tên vật tư có khoảng trắng thừa phải được cắt bỏ khi lưu.</summary>
    [Test]
    public async Task HandleAsync_NameWithWhitespace_TrimsName()
    {
        var result = await _handler.HandleAsync(MakeRequest("VT020") with { Name = "  Chỉ nha khoa  " });

        result.Name.Should().Be("Chỉ nha khoa");
    }
}
