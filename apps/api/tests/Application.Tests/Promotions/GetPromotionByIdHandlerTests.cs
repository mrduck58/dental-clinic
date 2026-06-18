using DentalClinic.API.Application.UseCases.Promotions;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Promotions;

[TestFixture]
public class GetPromotionByIdHandlerTests
{
    private IPromotionRepository _repo = null!;
    private IServiceRepository _serviceRepo = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = Substitute.For<IPromotionRepository>();
        _serviceRepo = Substitute.For<IServiceRepository>();
        _serviceRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Service>());
    }

    /// <summary>
    /// Lấy khuyến mãi theo ID hợp lệ phải trả về DTO với thông tin đầy đủ.
    /// </summary>
    [Test]
    public async Task HandleAsync_ExistingPromotion_ReturnsDto()
    {
        var promo = MakePromotion("SALE10");
        _repo.GetByIdAsync(promo.Id, Arg.Any<CancellationToken>()).Returns(promo);
        var handler = new GetPromotionByIdHandler(_repo, _serviceRepo);

        var result = await handler.HandleAsync(promo.Id);

        result.Should().NotBeNull();
        result!.Code.Should().Be("SALE10");
    }

    /// <summary>
    /// ID không tồn tại phải trả về null (không ném exception),
    /// nhất quán với thiết kế nullable return của handler.
    /// </summary>
    [Test]
    public async Task HandleAsync_NotFound_ReturnsNull()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Promotion?)null);
        var handler = new GetPromotionByIdHandler(_repo, _serviceRepo);

        var result = await handler.HandleAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    private static Promotion MakePromotion(string code = "SALE10")
        => Promotion.Create(code, "Khuyến mãi test", "Mô tả", "Percentage", 10m,
            new List<Guid>(),
            DateOnly.FromDateTime(DateTime.Today),
            DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
            true);
}
