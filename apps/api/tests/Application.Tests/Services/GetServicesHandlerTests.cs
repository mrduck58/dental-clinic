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

        var result = await handler.HandleAsync(null, null);

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

        var result = await handler.HandleAsync(status: "Active", null);

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

        var result = await handler.HandleAsync(null, search: "răng");

        result.Should().HaveCount(2);
    }
}
