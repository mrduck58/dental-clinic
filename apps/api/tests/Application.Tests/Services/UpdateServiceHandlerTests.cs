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
public class UpdateServiceHandlerTests
{
    private IServiceRepository _repo = null!;

    [SetUp]
    public void SetUp() => _repo = Substitute.For<IServiceRepository>();

    /// <summary>
    /// Cập nhật dịch vụ tồn tại phải gọi UpdateAsync và trả về DTO với thông tin mới.
    /// </summary>
    [Test]
    public async Task HandleAsync_ExistingService_CallsUpdateAsyncAndReturnsUpdatedDto()
    {
        var service = Service.Create("Nhổ răng", 200000m, 30, "Mô tả", null);
        _repo.GetByIdAsync(service.Id, Arg.Any<CancellationToken>()).Returns(service);
        var handler = new UpdateServiceHandler(_repo);

        var result = await handler.HandleAsync(service.Id, new UpdateServiceRequest("Nhổ răng khôn", 300000m, 45, "Mô tả mới", "https://img.jpg"));

        await _repo.Received(1).UpdateAsync(service, Arg.Any<CancellationToken>());
        result.Name.Should().Be("Nhổ răng khôn");
        result.Price.Should().Be(300000m);
    }

    /// <summary>
    /// Cập nhật với imageUrl=null không được ghi đè ảnh hiện tại,
    /// tránh mất ảnh khi client không gửi URL.
    /// </summary>
    [Test]
    public async Task HandleAsync_NullImageUrl_DoesNotOverwriteExistingImage()
    {
        var service = Service.Create("Nhổ răng", 200000m, 30, "Mô tả", "https://existing.jpg");
        _repo.GetByIdAsync(service.Id, Arg.Any<CancellationToken>()).Returns(service);
        var handler = new UpdateServiceHandler(_repo);

        var result = await handler.HandleAsync(service.Id, new UpdateServiceRequest("Tên mới", 100000m, 30, "Mô tả", null));

        result.ImageUrl.Should().Be("https://existing.jpg");
    }

    /// <summary>
    /// Dịch vụ không tồn tại phải ném NotFoundException.
    /// </summary>
    [Test]
    public async Task HandleAsync_NotFound_ThrowsNotFoundException()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Service?)null);
        var handler = new UpdateServiceHandler(_repo);

        Func<Task> act = () => handler.HandleAsync(Guid.NewGuid(), new UpdateServiceRequest("Tên", 100m, 30, "Mô tả", null));

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
