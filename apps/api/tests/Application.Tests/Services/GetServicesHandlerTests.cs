using DentalClinic.API.Application.UseCases.Services;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Services;

[TestFixture]
public class GetServicesHandlerTests
{
    private IServiceRepository _repo = null!;

    [SetUp]
    public void SetUp() => _repo = Substitute.For<IServiceRepository>();

    /// <summary>
    /// Không có filter trả về toàn bộ danh sách dịch vụ.
    /// </summary>
    [Test]
    public async Task HandleAsync_NoFilters_ReturnsAll()
    {
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Service>
        {
            Service.Create("A", 100m, 30, "Mô tả", null),
            Service.Create("B", 200m, 60, "Mô tả", null),
            Service.Create("C", 300m, 90, "Mô tả", null),
        });
        var handler = new GetServicesHandler(_repo);

        var result = await handler.Handle(new GetServicesQuery(null, null), CancellationToken.None);

        result.Should().HaveCount(3);
    }

    /// <summary>
    /// Filter status="Active" chỉ trả về dịch vụ đang hoạt động.
    /// </summary>
    [Test]
    public async Task HandleAsync_FilterByActiveStatus_ReturnsOnlyActive()
    {
        var active = Service.Create("Dịch vụ A", 100m, 30, "Mô tả", null);
        var inactive = Service.Create("Dịch vụ B", 200m, 60, "Mô tả", null);
        inactive.SetActive(false);
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Service> { active, inactive });
        var handler = new GetServicesHandler(_repo);

        var result = await handler.Handle(new GetServicesQuery(Status: "Active", Search: null), CancellationToken.None);

        result.Should().HaveCount(1);
        result.First().IsActive.Should().BeTrue();
    }

    /// <summary>
    /// Tìm kiếm theo tên dịch vụ không phân biệt hoa thường.
    /// </summary>
    [Test]
    public async Task HandleAsync_SearchByName_ReturnsMatchingServices()
    {
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Service>
        {
            Service.Create("Nhổ răng", 100m, 30, "Mô tả", null),
            Service.Create("Trồng răng sứ", 200m, 60, "Mô tả", null),
            Service.Create("Tẩy trắng", 300m, 45, "Mô tả", null),
        });
        var handler = new GetServicesHandler(_repo);

        var result = await handler.Handle(new GetServicesQuery(Status: null, Search: "răng"), CancellationToken.None);

        result.Should().HaveCount(2);
    }

    /// <summary>
    /// Filter status="Inactive" (bất kỳ giá trị khác "Active") chỉ trả về dịch vụ ngừng hoạt động.
    /// </summary>
    [Test]
    public async Task HandleAsync_FilterByInactiveStatus_ReturnsOnlyInactive()
    {
        var active = Service.Create("Dịch vụ A", 100m, 30, "Mô tả", null);
        var inactive = Service.Create("Dịch vụ B", 200m, 60, "Mô tả", null);
        inactive.SetActive(false);
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Service> { active, inactive });
        var handler = new GetServicesHandler(_repo);

        var result = await handler.Handle(new GetServicesQuery(Status: "Inactive", Search: null), CancellationToken.None);

        result.Should().HaveCount(1);
        result.First().IsActive.Should().BeFalse();
    }

    /// <summary>
    /// Filter status phải không phân biệt hoa thường (vd. "active" chữ thường vẫn khớp "Active").
    /// </summary>
    [Test]
    public async Task HandleAsync_StatusFilterLowerCase_StillMatchesActive()
    {
        var active = Service.Create("Dịch vụ A", 100m, 30, "Mô tả", null);
        var inactive = Service.Create("Dịch vụ B", 200m, 60, "Mô tả", null);
        inactive.SetActive(false);
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Service> { active, inactive });
        var handler = new GetServicesHandler(_repo);

        var result = await handler.Handle(new GetServicesQuery(Status: "active", Search: null), CancellationToken.None);

        result.Should().HaveCount(1);
        result.First().IsActive.Should().BeTrue();
    }

    /// <summary>
    /// Kết hợp đồng thời filter status và search phải áp dụng cả hai điều kiện (AND).
    /// </summary>
    [Test]
    public async Task HandleAsync_CombinedStatusAndSearchFilters_AppliesBoth()
    {
        var activeMatch = Service.Create("Nhổ răng khôn", 100m, 30, "Mô tả", null);
        var inactiveMatch = Service.Create("Nhổ răng sữa", 150m, 20, "Mô tả", null);
        inactiveMatch.SetActive(false);
        var activeNoMatch = Service.Create("Tẩy trắng", 300m, 45, "Mô tả", null);
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Service> { activeMatch, inactiveMatch, activeNoMatch });
        var handler = new GetServicesHandler(_repo);

        var result = await handler.Handle(new GetServicesQuery(Status: "Active", Search: "răng"), CancellationToken.None);

        result.Should().ContainSingle();
        result.First().Name.Should().Be("Nhổ răng khôn");
    }

    /// <summary>
    /// Tìm kiếm khớp theo Description khi tên không chứa từ khóa.
    /// </summary>
    [Test]
    public async Task HandleAsync_SearchMatchesDescription_ReturnsMatchingServices()
    {
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Service>
        {
            Service.Create("Implant", 100m, 30, "Cấy ghép răng implant cao cấp", null),
            Service.Create("Tẩy trắng", 300m, 45, "Làm sáng màu răng", null),
        });
        var handler = new GetServicesHandler(_repo);

        var result = await handler.Handle(new GetServicesQuery(Status: null, Search: "cấy ghép"), CancellationToken.None);

        result.Should().ContainSingle();
        result.First().Name.Should().Be("Implant");
    }

    /// <summary>
    /// Từ khóa tìm kiếm không khớp bất kỳ dịch vụ nào phải trả về danh sách rỗng.
    /// </summary>
    [Test]
    public async Task HandleAsync_SearchNoMatches_ReturnsEmptyList()
    {
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Service>
        {
            Service.Create("Nhổ răng", 100m, 30, "Mô tả", null),
        });
        var handler = new GetServicesHandler(_repo);

        var result = await handler.Handle(new GetServicesQuery(Status: null, Search: "không tồn tại"), CancellationToken.None);

        result.Should().BeEmpty();
    }

    /// <summary>
    /// Filter chỉ chứa khoảng trắng phải được xem như không có filter (trả về toàn bộ).
    /// </summary>
    [Test]
    public async Task HandleAsync_WhitespaceOnlyFilters_AreIgnored()
    {
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Service>
        {
            Service.Create("A", 100m, 30, "Mô tả", null),
            Service.Create("B", 200m, 60, "Mô tả", null),
        });
        var handler = new GetServicesHandler(_repo);

        var result = await handler.Handle(new GetServicesQuery(Status: "   ", Search: "   "), CancellationToken.None);

        result.Should().HaveCount(2);
    }
}
