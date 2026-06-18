using DentalClinic.API.Application.DTOs.Services;
using DentalClinic.API.Application.UseCases.Services;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Services;

[TestFixture]
public class ServiceHandlerTests
{
    private IServiceRepository _repo = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = Substitute.For<IServiceRepository>();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CreateServiceHandler
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Tạo dịch vụ hợp lệ phải gọi AddAsync 1 lần và trả về ServiceDto với đầy đủ thông tin.
    /// </summary>
    [Test]
    public async Task Create_ValidRequest_CallsAddAsyncAndReturnsDto()
    {
        var handler = new CreateServiceHandler(_repo);

        var result = await handler.HandleAsync(BuildCreateRequest("Nhổ răng", 200000m));

        await _repo.Received(1).AddAsync(Arg.Any<Service>(), Arg.Any<CancellationToken>());
        result.Name.Should().Be("Nhổ răng");
        result.Price.Should().Be(200000m);
    }

    /// <summary>
    /// Dịch vụ mới tạo mặc định phải có IsActive = true.
    /// </summary>
    [Test]
    public async Task Create_NewService_IsActiveByDefault()
    {
        var handler = new CreateServiceHandler(_repo);

        var result = await handler.HandleAsync(BuildCreateRequest("Trồng răng sứ", 5000000m));

        result.IsActive.Should().BeTrue();
    }

    /// <summary>
    /// Tạo dịch vụ với imageUrl phải lưu URL vào DTO.
    /// </summary>
    [Test]
    public async Task Create_WithImageUrl_ReturnsImageUrl()
    {
        var handler = new CreateServiceHandler(_repo);
        var req = new CreateServiceRequest("Tẩy trắng răng", 800000m, 60, "Mô tả", "https://img.com/teeth.jpg");

        var result = await handler.HandleAsync(req);

        result.ImageUrl.Should().Be("https://img.com/teeth.jpg");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // UpdateServiceHandler
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Cập nhật dịch vụ tồn tại phải gọi UpdateAsync 1 lần và trả về DTO với thông tin mới.
    /// </summary>
    [Test]
    public async Task Update_ExistingService_CallsUpdateAsyncAndReturnsUpdatedDto()
    {
        var service = MakeService("Nhổ răng", 200000m);
        _repo.GetByIdAsync(service.Id, Arg.Any<CancellationToken>()).Returns(service);
        var handler = new UpdateServiceHandler(_repo);

        var result = await handler.HandleAsync(service.Id, BuildUpdateRequest("Nhổ răng khôn", 300000m, "https://new.jpg"));

        await _repo.Received(1).UpdateAsync(service, Arg.Any<CancellationToken>());
        result.Name.Should().Be("Nhổ răng khôn");
        result.Price.Should().Be(300000m);
    }

    /// <summary>
    /// Cập nhật với imageUrl null không được ghi đè ảnh hiện tại,
    /// chỉ update các trường khác — tránh mất ảnh khi client không gửi URL.
    /// </summary>
    [Test]
    public async Task Update_NullImageUrl_DoesNotOverwriteExistingImage()
    {
        var service = MakeService(imageUrl: "https://existing.jpg");
        _repo.GetByIdAsync(service.Id, Arg.Any<CancellationToken>()).Returns(service);
        var handler = new UpdateServiceHandler(_repo);

        var result = await handler.HandleAsync(service.Id, BuildUpdateRequest("Tên mới", 100000m, null));

        result.ImageUrl.Should().Be("https://existing.jpg");
    }

    /// <summary>
    /// Cập nhật dịch vụ không tồn tại phải ném NotFoundException.
    /// </summary>
    [Test]
    public async Task Update_NotFound_ThrowsNotFoundException()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Service?)null);
        var handler = new UpdateServiceHandler(_repo);

        Func<Task> act = () => handler.HandleAsync(Guid.NewGuid(), BuildUpdateRequest("Tên", 100m, null));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // DeleteServiceHandler
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Xóa dịch vụ tồn tại phải gọi DeleteAsync 1 lần với đúng entity.
    /// </summary>
    [Test]
    public async Task Delete_ExistingService_CallsDeleteAsyncOnce()
    {
        var service = MakeService();
        _repo.GetByIdAsync(service.Id, Arg.Any<CancellationToken>()).Returns(service);
        var handler = new DeleteServiceHandler(_repo);

        await handler.HandleAsync(service.Id);

        await _repo.Received(1).DeleteAsync(service, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Xóa dịch vụ không tồn tại phải ném NotFoundException, không gọi DeleteAsync.
    /// </summary>
    [Test]
    public async Task Delete_NotFound_ThrowsNotFoundException()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Service?)null);
        var handler = new DeleteServiceHandler(_repo);

        Func<Task> act = () => handler.HandleAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
        await _repo.DidNotReceive().DeleteAsync(Arg.Any<Service>(), Arg.Any<CancellationToken>());
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ToggleServiceStatusHandler
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Dịch vụ đang Active phải chuyển sang Inactive sau khi toggle.
    /// </summary>
    [Test]
    public async Task Toggle_ActiveService_BecomesInactive()
    {
        var service = MakeService();
        _repo.GetByIdAsync(service.Id, Arg.Any<CancellationToken>()).Returns(service);
        var handler = new ToggleServiceStatusHandler(_repo);

        var result = await handler.HandleAsync(service.Id);

        result.IsActive.Should().BeFalse();
    }

    /// <summary>
    /// Dịch vụ đang Inactive phải chuyển sang Active sau khi toggle.
    /// </summary>
    [Test]
    public async Task Toggle_InactiveService_BecomesActive()
    {
        var service = MakeService();
        service.SetActive(false);
        _repo.GetByIdAsync(service.Id, Arg.Any<CancellationToken>()).Returns(service);
        var handler = new ToggleServiceStatusHandler(_repo);

        var result = await handler.HandleAsync(service.Id);

        result.IsActive.Should().BeTrue();
    }

    /// <summary>
    /// Toggle dịch vụ không tồn tại phải ném NotFoundException.
    /// </summary>
    [Test]
    public async Task Toggle_NotFound_ThrowsNotFoundException()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Service?)null);
        var handler = new ToggleServiceStatusHandler(_repo);

        Func<Task> act = () => handler.HandleAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetServicesHandler
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Không có filter trả về toàn bộ danh sách dịch vụ.
    /// </summary>
    [Test]
    public async Task GetServices_NoFilters_ReturnsAll()
    {
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Service>
        {
            MakeService("A"), MakeService("B"), MakeService("C"),
        });
        var handler = new GetServicesHandler(_repo);

        var result = await handler.HandleAsync(null, null);

        result.Should().HaveCount(3);
    }

    /// <summary>
    /// Filter status="Active" chỉ trả về dịch vụ đang hoạt động.
    /// </summary>
    [Test]
    public async Task GetServices_FilterByActiveStatus_ReturnsOnlyActive()
    {
        var active = MakeService("Dịch vụ A");
        var inactive = MakeService("Dịch vụ B");
        inactive.SetActive(false);
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Service> { active, inactive });
        var handler = new GetServicesHandler(_repo);

        var result = await handler.HandleAsync(status: "Active", null);

        result.Should().HaveCount(1);
        result.First().IsActive.Should().BeTrue();
    }

    /// <summary>
    /// Tìm kiếm theo tên dịch vụ không phân biệt hoa thường.
    /// </summary>
    [Test]
    public async Task GetServices_SearchByName_ReturnsMatchingServices()
    {
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Service>
        {
            MakeService("Nhổ răng"),
            MakeService("Trồng răng sứ"),
            MakeService("Tẩy trắng"),
        });
        var handler = new GetServicesHandler(_repo);

        var result = await handler.HandleAsync(null, search: "răng");

        result.Should().HaveCount(2);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetServiceByIdHandler
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Lấy dịch vụ theo ID hợp lệ phải trả về DTO với đúng thông tin.
    /// </summary>
    [Test]
    public async Task GetById_ExistingService_ReturnsDto()
    {
        var service = MakeService("Implant");
        _repo.GetByIdAsync(service.Id, Arg.Any<CancellationToken>()).Returns(service);
        var handler = new GetServiceByIdHandler(_repo);

        var result = await handler.HandleAsync(service.Id);

        result.Id.Should().Be(service.Id);
        result.Name.Should().Be("Implant");
    }

    /// <summary>
    /// ID không tồn tại phải ném NotFoundException.
    /// </summary>
    [Test]
    public async Task GetById_NotFound_ThrowsNotFoundException()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Service?)null);
        var handler = new GetServiceByIdHandler(_repo);

        Func<Task> act = () => handler.HandleAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════════════════════

    private static Service MakeService(string name = "Dịch Vụ Test", decimal price = 100000m, string? imageUrl = null)
        => Service.Create(name, price, 30, "Mô tả dịch vụ", imageUrl);

    private static CreateServiceRequest BuildCreateRequest(string name, decimal price)
        => new(name, price, 30, "Mô tả", null);

    private static UpdateServiceRequest BuildUpdateRequest(string name, decimal price, string? imageUrl)
        => new(name, price, 30, "Mô tả cập nhật", imageUrl);
}
