using DentalClinic.API.Application.UseCases.Promotions;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Promotions;

[TestFixture]
public class DeletePromotionHandlerTests
{
    private IPromotionRepository _repo = null!;

    [SetUp]
    public void SetUp() => _repo = Substitute.For<IPromotionRepository>();

    /// <summary>
    /// Xóa khuyến mãi tồn tại phải gọi DeleteAsync và trả về true.
    /// </summary>
    [Test]
    public async Task HandleAsync_ExistingPromotion_CallsDeleteAndReturnsTrue()
    {
        var promo = MakePromotion();
        _repo.GetByIdAsync(promo.Id, Arg.Any<CancellationToken>()).Returns(promo);
        var handler = new DeletePromotionHandler(_repo);

        var result = await handler.HandleAsync(promo.Id);

        await _repo.Received(1).DeleteAsync(promo, Arg.Any<CancellationToken>());
        result.Should().BeTrue();
    }

    /// <summary>
    /// Xóa khuyến mãi không tồn tại phải trả về false mà không ném exception,
    /// nhất quán với thiết kế của UpdatePromotionHandler.
    /// </summary>
    [Test]
    public async Task HandleAsync_NotFound_ReturnsFalseWithoutException()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Promotion?)null);
        var handler = new DeletePromotionHandler(_repo);

        var result = await handler.HandleAsync(Guid.NewGuid());

        result.Should().BeFalse();
        await _repo.DidNotReceive().DeleteAsync(Arg.Any<Promotion>(), Arg.Any<CancellationToken>());
    }

    private static Promotion MakePromotion()
        => Promotion.Create("SALE10", "Khuyến mãi test", "Mô tả", "Percentage", 10m,
            new List<Guid>(),
            DateOnly.FromDateTime(DateTime.Today),
            DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
            true);
}
