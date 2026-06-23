using DentalClinic.API.Application.DTOs.Promotions;
using DentalClinic.API.Application.UseCases.Promotions;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Promotions;

[TestFixture]
public class CreatePromotionHandlerTests
{
    private IPromotionRepository _repo = null!;

    [SetUp]
    public void SetUp() => _repo = Substitute.For<IPromotionRepository>();

    /// <summary>
    /// Tạo khuyến mãi hợp lệ phải gọi AddAsync 1 lần và trả về Guid của khuyến mãi mới.
    /// </summary>
    [Test]
    public async Task HandleAsync_ValidRequest_CallsAddAsyncAndReturnsGuid()
    {
        var handler = new CreatePromotionHandler(_repo);

        var result = await handler.HandleAsync(BuildCreateRequest("SALE10"));

        await _repo.Received(1).AddAsync(Arg.Any<Promotion>(), Arg.Any<CancellationToken>());
        result.Should().NotBe(Guid.Empty);
    }

    /// <summary>
    /// Code khuyến mãi phải được tự động chuyển thành chữ hoa,
    /// đảm bảo nhất quán khi so sánh code khi áp dụng.
    /// </summary>
    [Test]
    public async Task HandleAsync_LowercaseCode_CodeStoredUpperCase()
    {
        Promotion? captured = null;
        await _repo.AddAsync(Arg.Do<Promotion>(p => captured = p), Arg.Any<CancellationToken>());
        var handler = new CreatePromotionHandler(_repo);

        await handler.HandleAsync(BuildCreateRequest("sale10"));

        captured!.Code.Should().Be("SALE10");
    }

    private static CreatePromotionRequest BuildCreateRequest(string code)
        => new(code, "Tên khuyến mãi", "Mô tả", "Percentage", 10m,
            new List<Guid>(),
            DateOnly.FromDateTime(DateTime.Today),
            DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
            true);
}
