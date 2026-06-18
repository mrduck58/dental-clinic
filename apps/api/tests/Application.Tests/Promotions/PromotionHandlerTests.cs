using DentalClinic.API.Application.DTOs.Promotions;
using DentalClinic.API.Application.UseCases.Promotions;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Promotions;

[TestFixture]
public class PromotionHandlerTests
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

    // ═══════════════════════════════════════════════════════════════════════════
    // CreatePromotionHandler
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Tạo khuyến mãi hợp lệ phải gọi AddAsync 1 lần và trả về Guid của khuyến mãi mới.
    /// </summary>
    [Test]
    public async Task Create_ValidRequest_CallsAddAsyncAndReturnsGuid()
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
    public async Task Create_LowercaseCode_CodeStoredUpperCase()
    {
        Promotion? captured = null;
        await _repo.AddAsync(Arg.Do<Promotion>(p => captured = p), Arg.Any<CancellationToken>());
        var handler = new CreatePromotionHandler(_repo);

        await handler.HandleAsync(BuildCreateRequest("sale10"));

        captured!.Code.Should().Be("SALE10");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // UpdatePromotionHandler (returns bool)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Cập nhật khuyến mãi tồn tại phải gọi UpdateAsync và trả về true.
    /// </summary>
    [Test]
    public async Task Update_ExistingPromotion_CallsUpdateAndReturnsTrue()
    {
        var promo = MakePromotion("SALE10");
        _repo.GetByIdAsync(promo.Id, Arg.Any<CancellationToken>()).Returns(promo);
        var handler = new UpdatePromotionHandler(_repo);

        var result = await handler.HandleAsync(promo.Id, BuildUpdateRequest("SALE20"));

        await _repo.Received(1).UpdateAsync(promo, Arg.Any<CancellationToken>());
        result.Should().BeTrue();
    }

    /// <summary>
    /// Cập nhật khuyến mãi không tồn tại phải trả về false mà không ném exception,
    /// thiết kế này cho phép client kiểm tra kết quả thay vì bắt exception.
    /// </summary>
    [Test]
    public async Task Update_NotFound_ReturnsFalseWithoutException()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Promotion?)null);
        var handler = new UpdatePromotionHandler(_repo);

        var result = await handler.HandleAsync(Guid.NewGuid(), BuildUpdateRequest("CODE"));

        result.Should().BeFalse();
        await _repo.DidNotReceive().UpdateAsync(Arg.Any<Promotion>(), Arg.Any<CancellationToken>());
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // DeletePromotionHandler (returns bool)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Xóa khuyến mãi tồn tại phải gọi DeleteAsync và trả về true.
    /// </summary>
    [Test]
    public async Task Delete_ExistingPromotion_CallsDeleteAndReturnsTrue()
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
    /// thiết kế này nhất quán với UpdatePromotionHandler.
    /// </summary>
    [Test]
    public async Task Delete_NotFound_ReturnsFalseWithoutException()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Promotion?)null);
        var handler = new DeletePromotionHandler(_repo);

        var result = await handler.HandleAsync(Guid.NewGuid());

        result.Should().BeFalse();
        await _repo.DidNotReceive().DeleteAsync(Arg.Any<Promotion>(), Arg.Any<CancellationToken>());
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TogglePromotionStatusHandler
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Toggle khuyến mãi đang Active phải trả về DTO với IsActive = false.
    /// </summary>
    [Test]
    public async Task Toggle_ActivePromotion_ReturnsInactiveDto()
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
    public async Task Toggle_InactivePromotion_ReturnsActiveDto()
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
    public async Task Toggle_NotFound_ReturnsNull()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Promotion?)null);
        var handler = new TogglePromotionStatusHandler(_repo, _serviceRepo);

        var result = await handler.HandleAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetPromotionsHandler (cần cả 2 repo)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Danh sách khuyến mãi không áp dụng cho dịch vụ cụ thể (ServiceIds rỗng)
    /// phải hiển thị "Tất cả dịch vụ" trong ServiceNames.
    /// </summary>
    [Test]
    public async Task GetPromotions_NoServiceIds_ShowsAllServicesLabel()
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
    public async Task GetPromotions_WithMatchingServiceIds_ReturnsServiceNames()
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
    public async Task GetPromotions_CallsBothRepositories()
    {
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Promotion>());
        var handler = new GetPromotionsHandler(_repo, _serviceRepo);

        await handler.HandleAsync();

        await _repo.Received(1).GetAllAsync(Arg.Any<CancellationToken>());
        await _serviceRepo.Received(1).GetAllAsync(Arg.Any<CancellationToken>());
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetPromotionByIdHandler (cần cả 2 repo)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Lấy khuyến mãi theo ID hợp lệ phải trả về DTO với thông tin đầy đủ.
    /// </summary>
    [Test]
    public async Task GetById_ExistingPromotion_ReturnsDto()
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
    public async Task GetById_NotFound_ReturnsNull()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Promotion?)null);
        var handler = new GetPromotionByIdHandler(_repo, _serviceRepo);

        var result = await handler.HandleAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════════════════════

    private static Promotion MakePromotion(
        string code = "SALE10",
        bool isActive = true,
        List<Guid>? serviceIds = null)
        => Promotion.Create(
            code, "Khuyến mãi test", "Mô tả",
            "Percentage", 10m,
            serviceIds ?? new List<Guid>(),
            DateOnly.FromDateTime(DateTime.Today),
            DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
            isActive);

    private static CreatePromotionRequest BuildCreateRequest(string code)
        => new(code, "Tên khuyến mãi", "Mô tả", "Percentage", 10m,
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
