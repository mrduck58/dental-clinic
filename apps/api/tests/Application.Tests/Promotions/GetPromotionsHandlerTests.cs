using DentalClinic.API.Application.UseCases.Promotions;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Promotions;

[TestFixture]
public class GetPromotionsHandlerTests
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
    /// Danh sách khuyến mãi không áp dụng cho dịch vụ cụ thể (ServiceIds rỗng)
    /// phải hiển thị "Tất cả dịch vụ" trong ServiceNames.
    /// </summary>
    [Test]
    public async Task HandleAsync_NoServiceIds_ShowsAllServicesLabel()
    {
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Promotion>
        {
            MakePromotion(serviceIds: new List<Guid>()),
        });
        var handler = new GetPromotionsHandler(_repo, _serviceRepo);

        var result = (await handler.HandleAsync()).ToList();

        result.Should().HaveCount(1);
        result[0].ServiceNames.Should().Contain(n => n.Contains("tat ca") || n.Contains("Tat ca") || n.Contains("tất cả"), because: "empty service ids means all services");
    }

    /// <summary>
    /// Khi ServiceIds có Guid khớp với dịch vụ trong serviceRepo, ServiceNames phải
    /// hiển thị tên dịch vụ thay vì Guid dạng chuỗi.
    /// </summary>
    [Test]
    public async Task HandleAsync_WithMatchingServiceIds_ReturnsServiceNames()
    {
        var service = Service.Create("Nhổ răng", 200000m, 30, "Mô tả", null);
        _serviceRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Service> { service });
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Promotion>
        {
            MakePromotion(serviceIds: new List<Guid> { service.Id }),
        });
        var handler = new GetPromotionsHandler(_repo, _serviceRepo);

        var result = (await handler.HandleAsync()).ToList();

        result[0].ServiceNames.Should().Contain("Nhổ răng");
    }

    /// <summary>
    /// GetPromotions phải gọi GetAllAsync của cả promotion repo và service repo.
    /// </summary>
    [Test]
    public async Task HandleAsync_CallsBothRepositories()
    {
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Promotion>());
        var handler = new GetPromotionsHandler(_repo, _serviceRepo);

        await handler.HandleAsync();

        await _repo.Received(1).GetAllAsync(Arg.Any<CancellationToken>());
        await _serviceRepo.Received(1).GetAllAsync(Arg.Any<CancellationToken>());
    }

    private static Promotion MakePromotion(List<Guid>? serviceIds = null)
        => Promotion.Create("SALE10", "Khuyến mãi test", "Mô tả", "Percentage", 10m,
            serviceIds ?? new List<Guid>(),
            DateOnly.FromDateTime(DateTime.Today),
            DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
            true);
}
