using DentalClinic.API.Application.UseCases.Services;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Services;

[TestFixture]
public class GetServiceByIdHandlerTests
{
    private IServiceRepository _repo = null!;

    [SetUp]
    public void SetUp() => _repo = Substitute.For<IServiceRepository>();

    /// <summary>
    /// Lấy dịch vụ theo ID hợp lệ phải trả về DTO với đúng thông tin.
    /// </summary>
    [Test]
    public async Task HandleAsync_ExistingService_ReturnsDto()
    {
        var service = Service.Create("Implant", 10000000m, 180, "Mô tả", null);
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
    public async Task HandleAsync_NotFound_ThrowsNotFoundException()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Service?)null);
        var handler = new GetServiceByIdHandler(_repo);

        Func<Task> act = () => handler.HandleAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
