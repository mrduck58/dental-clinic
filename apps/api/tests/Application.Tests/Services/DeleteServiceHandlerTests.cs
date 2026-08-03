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
public class DeleteServiceHandlerTests
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
    /// Xóa dịch vụ tồn tại phải gọi DeleteAsync 1 lần với đúng entity.
    /// </summary>
    [Test]
    public async Task HandleAsync_ExistingService_CallsDeleteAsyncOnce()
    {
        var service = Service.Create("Nhổ răng", 200000m, 30, "Mô tả", null);
        _repo.GetByIdAsync(service.Id, Arg.Any<CancellationToken>()).Returns(service);
        var handler = new DeleteServiceHandler(_repo, _activityLog, _currentUser);

        await handler.Handle(new DeleteServiceCommand(service.Id), CancellationToken.None);

        await _repo.Received(1).DeleteAsync(service, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Dịch vụ không tồn tại phải ném NotFoundException, không gọi DeleteAsync.
    /// </summary>
    [Test]
    public async Task HandleAsync_NotFound_ThrowsNotFoundException()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Service?)null);
        var handler = new DeleteServiceHandler(_repo, _activityLog, _currentUser);

        Func<Task> act = () => handler.Handle(new DeleteServiceCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await _repo.DidNotReceive().DeleteAsync(Arg.Any<Service>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Xóa dịch vụ tồn tại phải ghi log hoạt động với action Delete, module Service.
    /// </summary>
    [Test]
    public async Task HandleAsync_ExistingService_LogsActivityWithDeleteAction()
    {
        var service = Service.Create("Nhổ răng", 200000m, 30, "Mô tả", null);
        _repo.GetByIdAsync(service.Id, Arg.Any<CancellationToken>()).Returns(service);
        var handler = new DeleteServiceHandler(_repo, _activityLog, _currentUser);

        await handler.Handle(new DeleteServiceCommand(service.Id), CancellationToken.None);

        await _activityLog.Received(1).LogAsync(
            userId: Arg.Any<Guid?>(),
            userName: Arg.Any<string>(),
            userRole: Arg.Any<string>(),
            action: ActivityAction.Delete,
            module: ActivityModule.Service,
            description: Arg.Is<string>(d => d.Contains("Nhổ răng")),
            status: ActivityStatus.Success,
            ipAddress: Arg.Any<string?>(),
            targetId: service.Id.ToString(),
            ct: Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Dịch vụ không tồn tại không được ghi log hoạt động (không có hành động nào xảy ra).
    /// </summary>
    [Test]
    public async Task HandleAsync_NotFound_DoesNotLogActivity()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Service?)null);
        var handler = new DeleteServiceHandler(_repo, _activityLog, _currentUser);

        Func<Task> act = () => handler.Handle(new DeleteServiceCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await _activityLog.DidNotReceive().LogAsync(
            Arg.Any<Guid?>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }
}
