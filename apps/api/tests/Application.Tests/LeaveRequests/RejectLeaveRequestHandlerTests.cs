using DentalClinic.API.Application.DTOs.LeaveRequests;
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
public class RejectLeaveRequestHandlerTests
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
    /// Từ chối đơn Pending phải lưu reviewer note và trả về status Rejected.
    /// </summary>
    [Test]
    public async Task HandleAsync_PendingRequest_RejectsWithReviewerNote()
    {
        var lr = MakeRequest();
        _repo.GetByIdAsync(lr.Id, Arg.Any<CancellationToken>()).Returns(lr);
        var handler = new RejectLeaveRequestHandler(_repo, _activityLog, _notification, _currentUser);

        var result = await handler.Handle(new RejectLeaveRequestCommand(lr.Id, new RejectLeaveRequestRequest("Không đủ điều kiện")), CancellationToken.None);

        result.Status.Should().Be("Rejected");
        result.ReviewerNote.Should().Be("Không đủ điều kiện");
    }

    /// <summary>
    /// Từ chối đơn không tồn tại phải ném NotFoundException.
    /// </summary>
    [Test]
    public async Task HandleAsync_NotFound_ThrowsNotFoundException()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((LeaveRequest?)null);
        var handler = new RejectLeaveRequestHandler(_repo, _activityLog, _notification, _currentUser);

        Func<Task> act = () => handler.Handle(new RejectLeaveRequestCommand(Guid.NewGuid(), new RejectLeaveRequestRequest(null)), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>
    /// Từ chối đơn đã Rejected rồi phải ném ValidationException từ entity.
    /// </summary>
    [Test]
    public async Task HandleAsync_AlreadyRejected_ThrowsValidationException()
    {
        var lr = MakeRequest();
        lr.Reject(null);
        _repo.GetByIdAsync(lr.Id, Arg.Any<CancellationToken>()).Returns(lr);
        var handler = new RejectLeaveRequestHandler(_repo, _activityLog, _notification, _currentUser);

        Func<Task> act = () => handler.Handle(new RejectLeaveRequestCommand(lr.Id, new RejectLeaveRequestRequest(null)), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>
    /// Từ chối đơn đã được duyệt (Approved) phải ném ValidationException,
    /// không cho phép từ chối đơn đã xử lý xong theo chiều ngược lại.
    /// </summary>
    [Test]
    public async Task HandleAsync_ApprovedRequest_ThrowsValidationException()
    {
        var lr = MakeRequest();
        lr.Approve();
        _repo.GetByIdAsync(lr.Id, Arg.Any<CancellationToken>()).Returns(lr);
        var handler = new RejectLeaveRequestHandler(_repo, _activityLog, _notification, _currentUser);

        Func<Task> act = () => handler.Handle(new RejectLeaveRequestCommand(lr.Id, new RejectLeaveRequestRequest(null)), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>
    /// Từ chối với ReviewerNote null/rỗng thì nội dung thông báo không được chứa
    /// phần "Lý do:" (nhánh string.IsNullOrWhiteSpace trong body thông báo).
    /// </summary>
    [Test]
    public async Task HandleAsync_NullReviewerNote_NotificationBodyExcludesReasonSegment()
    {
        var lr = MakeRequest();
        _repo.GetByIdAsync(lr.Id, Arg.Any<CancellationToken>()).Returns(lr);
        var handler = new RejectLeaveRequestHandler(_repo, _activityLog, _notification, _currentUser);

        await handler.Handle(new RejectLeaveRequestCommand(lr.Id, new RejectLeaveRequestRequest(null)), CancellationToken.None);

        await _notification.Received(1).CreateAsync(
            Arg.Is<CreateNotificationRequest>(n => !n.Body.Contains("Lý do:")),
            Arg.Any<CancellationToken>());
    }

    private static LeaveRequest MakeRequest()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var shifts = new List<(DateOnly Date, string ShiftId)>
        {
            (today, "08:00-10:00"),
            (today.AddDays(1), "08:00-10:00"),
            (today.AddDays(2), "08:00-10:00"),
        };
        var lr = LeaveRequest.Create(Guid.NewGuid(), LeaveType.Annual, shifts, "Lý do test");
        var user = User.Create("emp", "emp@test.com", "hash", UserRole.Staff, null, "Test");
        typeof(LeaveRequest).GetProperty("User")!.SetValue(lr, user);
        return lr;
    }
}
