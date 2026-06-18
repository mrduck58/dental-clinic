using DentalClinic.API.Application.UseCases.Services;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Services;

[TestFixture]
public class ToggleServiceStatusHandlerTests
{
    private IServiceRepository _repo = null!;

    [SetUp]
    public void SetUp() => _repo = Substitute.For<IServiceRepository>();

    /// <summary>
    /// Dịch vụ đang Active phải chuyển sang Inactive sau khi toggle.
    /// </summary>
    [Test]
    public async Task HandleAsync_ActiveService_BecomesInactive()
    {
        var service = Service.Create("Nhổ răng", 200000m, 30, "Mô tả", null);
        _repo.GetByIdAsync(service.Id, Arg.Any<CancellationToken>()).Returns(service);
        var handler = new ToggleServiceStatusHandler(_repo);

        var result = await handler.HandleAsync(service.Id);

        result.IsActive.Should().BeFalse();
    }

    /// <summary>
    /// Dịch vụ đang Inactive phải chuyển sang Active sau khi toggle.
    /// </summary>
    [Test]
    public async Task HandleAsync_InactiveService_BecomesActive()
    {
        var service = Service.Create("Nhổ răng", 200000m, 30, "Mô tả", null);
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
    public async Task HandleAsync_NotFound_ThrowsNotFoundException()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Service?)null);
        var handler = new ToggleServiceStatusHandler(_repo);

        Func<Task> act = () => handler.HandleAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
