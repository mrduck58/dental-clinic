using DentalClinic.API.Application.DTOs.Services;
using DentalClinic.API.Application.UseCases.Services;
using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Services;

[TestFixture]
public class UpdateServiceHandlerTests
{
    private IServiceRepository _repo = null!;
    private IActivityLogService _activityLog = null!;
    private ICurrentUserService _currentUser = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = Substitute.For<IServiceRepository>();
        _activityLog = Substitute.For<IActivityLogService>();
        _currentUser = Substitute.For<ICurrentUserService>();
    }

    /// <summary>
    /// Cập nhật dịch vụ tồn tại phải gọi UpdateAsync và trả về DTO với thông tin mới.
    /// </summary>
    [Test]
    public async Task HandleAsync_ExistingService_CallsUpdateAsyncAndReturnsUpdatedDto()
    {
        var service = Service.Create("Nhổ răng", 200000m, 30, "Mô tả", null);
        _repo.GetByIdAsync(service.Id, Arg.Any<CancellationToken>()).Returns(service);
        var handler = new UpdateServiceHandler(_repo, _activityLog, _currentUser);

        var result = await handler.Handle(new UpdateServiceCommand(service.Id, "Nhổ răng khôn", 300000m, 45, "Mô tả mới", "https://img.jpg", null), CancellationToken.None);

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
        var handler = new UpdateServiceHandler(_repo, _activityLog, _currentUser);

        var result = await handler.Handle(new UpdateServiceCommand(service.Id, "Tên mới", 100000m, 30, "Mô tả", null, null), CancellationToken.None);

        result.ImageUrl.Should().Be("https://existing.jpg");
    }

    /// <summary>
    /// Dịch vụ không tồn tại phải ném NotFoundException.
    /// </summary>
    [Test]
    public async Task HandleAsync_NotFound_ThrowsNotFoundException()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Service?)null);
        var handler = new UpdateServiceHandler(_repo, _activityLog, _currentUser);

        Func<Task> act = () => handler.Handle(new UpdateServiceCommand(Guid.NewGuid(), "Tên", 100m, 30, "Mô tả", null, null), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>
    /// Cập nhật dịch vụ tồn tại phải ghi log hoạt động với action Edit, module Service.
    /// </summary>
    [Test]
    public async Task HandleAsync_ExistingService_LogsActivityWithEditAction()
    {
        var service = Service.Create("Nhổ răng", 200000m, 30, "Mô tả", null);
        _repo.GetByIdAsync(service.Id, Arg.Any<CancellationToken>()).Returns(service);
        var handler = new UpdateServiceHandler(_repo, _activityLog, _currentUser);

        await handler.Handle(new UpdateServiceCommand(service.Id, "Nhổ răng khôn", 300000m, 45, "Mô tả mới", null, null), CancellationToken.None);

        await _activityLog.Received(1).LogAsync(
            userId: Arg.Any<Guid?>(),
            userName: Arg.Any<string>(),
            userRole: Arg.Any<string>(),
            action: ActivityAction.Edit,
            module: ActivityModule.Service,
            description: Arg.Is<string>(d => d.Contains("Nhổ răng khôn")),
            status: ActivityStatus.Success,
            ipAddress: Arg.Any<string?>(),
            targetId: service.Id.ToString(),
            ct: Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Cập nhật dịch vụ thành công phải thiết lập UpdatedAt (khác null), đánh dấu bản ghi đã được sửa đổi.
    /// </summary>
    [Test]
    public async Task HandleAsync_ExistingService_SetsUpdatedAtTimestamp()
    {
        var service = Service.Create("Nhổ răng", 200000m, 30, "Mô tả", null);
        service.UpdatedAt.Should().BeNull();
        _repo.GetByIdAsync(service.Id, Arg.Any<CancellationToken>()).Returns(service);
        var handler = new UpdateServiceHandler(_repo, _activityLog, _currentUser);

        var result = await handler.Handle(new UpdateServiceCommand(service.Id, "Tên mới", 100000m, 30, "Mô tả", null, null), CancellationToken.None);

        result.UpdatedAt.Should().NotBeNull();
    }
}
