using DentalClinic.API.Application.UseCases.Promotions;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Promotions;

[TestFixture]
public class TogglePromotionStatusHandlerTests
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
    /// Toggle khuyến mãi đang Active phải trả về DTO với IsActive = false.
    /// </summary>
    [Test]
    public async Task HandleAsync_ActivePromotion_ReturnsInactiveDto()
    {
        var promo = MakePromotion(isActive: true);
        _repo.GetByIdAsync(promo.Id, Arg.Any<CancellationToken>()).Returns(promo);
        var handler = new TogglePromotionStatusHandler(_repo, _serviceRepo);

        var result = await handler.HandleAsync(promo.Id);

        result.Should().NotBeNull();
        result!.IsActive.Should().BeFalse();
    }

    /// <summary>
    /// Toggle khuyến mãi đang Inactive phải trả về DTO với IsActive = true.
    /// </summary>
    [Test]
    public async Task HandleAsync_InactivePromotion_ReturnsActiveDto()
    {
        var promo = MakePromotion(isActive: false);
        _repo.GetByIdAsync(promo.Id, Arg.Any<CancellationToken>()).Returns(promo);
        var handler = new TogglePromotionStatusHandler(_repo, _serviceRepo);

        var result = await handler.HandleAsync(promo.Id);

        result!.IsActive.Should().BeTrue();
    }

    /// <summary>
    /// Toggle khuyến mãi không tồn tại phải trả về null.
    /// </summary>
    [Test]
    public async Task HandleAsync_NotFound_ReturnsNull()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Promotion?)null);
        var handler = new TogglePromotionStatusHandler(_repo, _serviceRepo);

        var result = await handler.HandleAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    /// <summary>
    /// Toggle trạng thái phải gọi UpdateAsync đúng 1 lần để lưu thay đổi vào database,
    /// tránh trường hợp chỉ đổi giá trị trong bộ nhớ mà không được persist.
    /// </summary>
    [Test]
    public async Task HandleAsync_ExistingPromotion_CallsUpdateAsyncOnce()
    {
        var promo = MakePromotion(isActive: true);
        _repo.GetByIdAsync(promo.Id, Arg.Any<CancellationToken>()).Returns(promo);
        var handler = new TogglePromotionStatusHandler(_repo, _serviceRepo);

        await handler.HandleAsync(promo.Id);

        await _repo.Received(1).UpdateAsync(promo, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// ServiceId trong khuyến mãi không còn khớp với dịch vụ nào trong hệ thống
    /// phải hiển thị dạng chuỗi Guid trong ServiceNames của DTO trả về sau khi toggle.
    /// </summary>
    [Test]
    public async Task HandleAsync_ServiceIdNotFoundInServiceRepo_FallsBackToGuidString()
    {
        var missingServiceId = Guid.NewGuid();
        var promo = Promotion.Create("SALE10", "Khuyến mãi test", "Mô tả", "Percentage", 10m,
            new List<Guid> { missingServiceId },
            DateOnly.FromDateTime(DateTime.Today),
            DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
            true);
        _repo.GetByIdAsync(promo.Id, Arg.Any<CancellationToken>()).Returns(promo);
        var handler = new TogglePromotionStatusHandler(_repo, _serviceRepo);

        var result = await handler.HandleAsync(promo.Id);

        result!.ServiceNames.Should().Contain(missingServiceId.ToString());
    }

    private static Promotion MakePromotion(bool isActive = true)
        => Promotion.Create("SALE10", "Khuyến mãi test", "Mô tả", "Percentage", 10m,
            new List<Guid>(),
            DateOnly.FromDateTime(DateTime.Today),
            DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
            isActive);
}
