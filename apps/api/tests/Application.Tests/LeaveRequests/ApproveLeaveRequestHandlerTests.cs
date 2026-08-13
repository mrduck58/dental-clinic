using DentalClinic.API.Application.UseCases.LeaveRequests;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.LeaveRequests;

[TestFixture]
public class ApproveLeaveRequestHandlerTests
{
    private ILeaveRequestRepository _repo = null!;
    private IActivityLogService _activityLog = null!;
    private INotificationService _notification = null!;
    private ICurrentUserService _currentUser = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = Substitute.For<ILeaveRequestRepository>();
        _activityLog = Substitute.For<IActivityLogService>();
        _notification = Substitute.For<INotificationService>();
        _currentUser = Substitute.For<ICurrentUserService>();
    }

    /// <summary>
    /// Duyệt đơn đang Pending phải gọi UpdateAsync và trả về DTO với status Approved.
    /// </summary>
    [Test]
    public async Task HandleAsync_PendingRequest_ApprovesAndReturnsApprovedStatus()
    {
        var lr = MakeRequest();
        _repo.GetByIdAsync(lr.Id, Arg.Any<CancellationToken>()).Returns(lr);
        var handler = new ApproveLeaveRequestHandler(_repo, _activityLog, _notification, _currentUser);

        var result = await handler.Handle(new ApproveLeaveRequestCommand(lr.Id), CancellationToken.None);

        await _repo.Received(1).UpdateAsync(lr, Arg.Any<CancellationToken>());
        result.Status.Should().Be("Approved");
    }

    /// <summary>
    /// Duyệt đơn không tồn tại phải ném NotFoundException.
    /// </summary>
    [Test]
    public async Task HandleAsync_NotFound_ThrowsNotFoundException()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((LeaveRequest?)null);
        var handler = new ApproveLeaveRequestHandler(_repo, _activityLog, _notification, _currentUser);

        Func<Task> act = () => handler.Handle(new ApproveLeaveRequestCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>
    /// Duyệt đơn đã Approved rồi phải ném ValidationException,
    /// không cho phép duyệt lại đơn đã xử lý.
    /// </summary>
    [Test]
    public async Task HandleAsync_AlreadyApproved_ThrowsValidationException()
    {
        var lr = MakeRequest();
        lr.Approve();
        _repo.GetByIdAsync(lr.Id, Arg.Any<CancellationToken>()).Returns(lr);
        var handler = new ApproveLeaveRequestHandler(_repo, _activityLog, _notification, _currentUser);

        Func<Task> act = () => handler.Handle(new ApproveLeaveRequestCommand(lr.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>
    /// Duyệt đơn đã bị từ chối (Rejected) phải ném ValidationException,
    /// vì đơn đã ở trạng thái kết thúc, không còn ở trạng thái chờ xử lý.
    /// </summary>
    [Test]
    public async Task HandleAsync_RejectedRequest_ThrowsValidationException()
    {
        var lr = MakeRequest();
        lr.Reject(null);
        _repo.GetByIdAsync(lr.Id, Arg.Any<CancellationToken>()).Returns(lr);
        var handler = new ApproveLeaveRequestHandler(_repo, _activityLog, _notification, _currentUser);

        Func<Task> act = () => handler.Handle(new ApproveLeaveRequestCommand(lr.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>
    /// Duyệt đơn thành công phải gửi thông báo cho đúng người nộp đơn (UserId của request),
    /// để nhân viên biết đơn xin nghỉ của mình đã được duyệt.
    /// </summary>
    [Test]
    public async Task HandleAsync_PendingRequest_SendsNotificationToRequester()
    {
        var lr = MakeRequest();
        _repo.GetByIdAsync(lr.Id, Arg.Any<CancellationToken>()).Returns(lr);
        var handler = new ApproveLeaveRequestHandler(_repo, _activityLog, _notification, _currentUser);

        await handler.Handle(new ApproveLeaveRequestCommand(lr.Id), CancellationToken.None);

        await _notification.Received(1).CreateAsync(
            Arg.Is<CreateNotificationRequest>(n => n.UserId == lr.UserId),
            Arg.Any<CancellationToken>());
    }

    private static LeaveRequest MakeRequest()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var lr = LeaveRequest.Create(Guid.NewGuid(), LeaveType.Annual, today, today.AddDays(2), "Lý do test");
        var user = User.Create("emp", "emp@test.com", "hash", UserRole.Staff, null, "Test");
        typeof(LeaveRequest).GetProperty("User")!.SetValue(lr, user);
        return lr;
    }
}
