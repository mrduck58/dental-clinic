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
public class CreatePromotionHandlerTests
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
    /// Tạo khuyến mãi hợp lệ phải gọi AddAsync 1 lần và trả về Guid của khuyến mãi mới.
    /// </summary>
    [Test]
    public async Task HandleAsync_ValidRequest_CallsAddAsyncAndReturnsGuid()
    {
        var handler = new CreatePromotionHandler(_repo, _activityLog, _currentUser);

        var result = await handler.Handle(BuildCreateRequest("SALE10"), CancellationToken.None);

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
        var handler = new CreatePromotionHandler(_repo, _activityLog, _currentUser);

        await handler.Handle(BuildCreateRequest("sale10"), CancellationToken.None);

        captured!.Code.Should().Be("SALE10");
    }

    private static CreatePromotionCommand BuildCreateRequest(string code)
        => new(code, "Tên khuyến mãi", "Mô tả", "Percentage", 10m,
            new List<Guid>(),
            DateOnly.FromDateTime(DateTime.Today),
            DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
            true);
}
