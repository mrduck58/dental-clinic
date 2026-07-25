using DentalClinic.API.Application.DTOs.Promotions;
using DentalClinic.API.Application.UseCases.Promotions;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Promotions;

[TestFixture]
public class UpdatePromotionHandlerTests
{
    private IPromotionRepository _repo = null!;
    private IActivityLogService _activityLog = null!;
    private ICurrentUserService _currentUser = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = Substitute.For<IPromotionRepository>();
        _activityLog = Substitute.For<IActivityLogService>();
        _currentUser = Substitute.For<ICurrentUserService>();
    }

    /// <summary>
    /// Cập nhật khuyến mãi tồn tại phải gọi UpdateAsync và trả về true.
    /// </summary>
    [Test]
    public async Task HandleAsync_ExistingPromotion_CallsUpdateAndReturnsTrue()
    {
        var promo = MakePromotion("SALE10");
        _repo.GetByIdAsync(promo.Id, Arg.Any<CancellationToken>()).Returns(promo);
        var handler = new UpdatePromotionHandler(_repo, _activityLog, _currentUser);

        var result = await handler.HandleAsync(promo.Id, BuildUpdateRequest("SALE20"));

        await _repo.Received(1).UpdateAsync(promo, Arg.Any<CancellationToken>());
        result.Should().BeTrue();
    }

    /// <summary>
    /// Cập nhật khuyến mãi không tồn tại phải trả về false mà không ném exception,
    /// thiết kế này cho phép client kiểm tra kết quả thay vì bắt exception.
    /// </summary>
    [Test]
    public async Task HandleAsync_NotFound_ReturnsFalseWithoutException()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Promotion?)null);
        var handler = new UpdatePromotionHandler(_repo, _activityLog, _currentUser);

        var result = await handler.HandleAsync(Guid.NewGuid(), BuildUpdateRequest("CODE"));

        result.Should().BeFalse();
        await _repo.DidNotReceive().UpdateAsync(Arg.Any<Promotion>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Code khuyến mãi mới nhập vào khi update phải được tự động chuyển thành chữ hoa,
    /// nhất quán với hành vi của CreatePromotionHandler khi so sánh code lúc áp dụng.
    /// </summary>
    [Test]
    public async Task HandleAsync_LowercaseCode_CodeStoredUpperCase()
    {
        var promo = MakePromotion("SALE10");
        _repo.GetByIdAsync(promo.Id, Arg.Any<CancellationToken>()).Returns(promo);
        var handler = new UpdatePromotionHandler(_repo, _activityLog, _currentUser);

        await handler.HandleAsync(promo.Id, BuildUpdateRequest("sale20"));

        promo.Code.Should().Be("SALE20");
    }

    private static Promotion MakePromotion(string code = "SALE10")
        => Promotion.Create(code, "Khuyến mãi test", "Mô tả", "Percentage", 10m,
            new List<Guid>(),
            DateOnly.FromDateTime(DateTime.Today),
            DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
            true);

    private static UpdatePromotionRequest BuildUpdateRequest(string code)
        => new(code, "Tên mới", "Mô tả mới", "Percentage", 20m,
            new List<Guid>(),
            DateOnly.FromDateTime(DateTime.Today),
            DateOnly.FromDateTime(DateTime.Today.AddDays(60)));
}
