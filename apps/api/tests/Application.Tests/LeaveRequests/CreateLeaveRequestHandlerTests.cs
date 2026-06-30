using DentalClinic.API.Application.DTOs.LeaveRequests;
using DentalClinic.API.Application.UseCases.LeaveRequests;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.LeaveRequests;

[TestFixture]
public class CreateLeaveRequestHandlerTests
{
    private ILeaveRequestRepository _repo = null!;
    private IActivityLogService _activityLog = null!;
    private ICurrentUserService _currentUser = null!;
    private INotificationService _notification = null!;
    private IUserRepository _userRepo = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = Substitute.For<ILeaveRequestRepository>();
        _activityLog = Substitute.For<IActivityLogService>();
        _currentUser = Substitute.For<ICurrentUserService>();
        _notification = Substitute.For<INotificationService>();
        _userRepo = Substitute.For<IUserRepository>();
    }

    /// <summary>
    /// Tạo đơn nghỉ phép hợp lệ phải gọi AddAsync 1 lần và trả về DTO với status Pending.
    /// </summary>
    [Test]
    public async Task HandleAsync_ValidRequest_CallsAddAsyncAndReturnsPending()
    {
        var user = User.Create("emp", "emp@test.com", "hash", "Staff", null, "Nhân Viên Test");
        _repo.When(r => r.AddAsync(Arg.Any<LeaveRequest>(), Arg.Any<CancellationToken>()))
            .Do(call => typeof(LeaveRequest).GetProperty("User")!.SetValue(call.Arg<LeaveRequest>(), user));
        _userRepo.GetUserIdsByRoleAsync("Owner", Arg.Any<CancellationToken>()).Returns([]);
        var handler = new CreateLeaveRequestHandler(_repo, _activityLog, _notification, _userRepo, _currentUser);

        var result = await handler.HandleAsync(Guid.NewGuid(), BuildRequest("Annual", "Lý do hợp lệ"));

        await _repo.Received(1).AddAsync(Arg.Any<LeaveRequest>(), Arg.Any<CancellationToken>());
        result.Status.Should().Be("Pending");
    }

    /// <summary>
    /// Lý do nghỉ phép để trống phải ném ValidationException trước khi gọi AddAsync.
    /// </summary>
    [Test]
    public async Task HandleAsync_EmptyReason_ThrowsValidationException()
    {
        var handler = new CreateLeaveRequestHandler(_repo, _activityLog, _notification, _userRepo, _currentUser);

        Func<Task> act = () => handler.HandleAsync(Guid.NewGuid(), BuildRequest("Annual", ""));

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>
    /// Lý do nghỉ phép vượt 1000 ký tự phải ném ValidationException.
    /// </summary>
    [Test]
    public async Task HandleAsync_ReasonOver1000Chars_ThrowsValidationException()
    {
        var handler = new CreateLeaveRequestHandler(_repo, _activityLog, _notification, _userRepo, _currentUser);

        Func<Task> act = () => handler.HandleAsync(Guid.NewGuid(), BuildRequest("Annual", new string('a', 1001)));

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>
    /// Loại nghỉ phép không hợp lệ (không nằm trong enum) phải ném ValidationException.
    /// </summary>
    [Test]
    public async Task HandleAsync_InvalidLeaveType_ThrowsValidationException()
    {
        var handler = new CreateLeaveRequestHandler(_repo, _activityLog, _notification, _userRepo, _currentUser);

        Func<Task> act = () => handler.HandleAsync(Guid.NewGuid(), BuildRequest("InvalidType", "Lý do"));

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>
    /// Ngày kết thúc trước ngày bắt đầu phải ném ValidationException từ entity.
    /// </summary>
    [Test]
    public async Task HandleAsync_EndDateBeforeStartDate_ThrowsValidationException()
    {
        var handler = new CreateLeaveRequestHandler(_repo, _activityLog, _notification, _userRepo, _currentUser);
        var req = new CreateLeaveRequestRequest(
            "Annual",
            DateOnly.FromDateTime(DateTime.Today.AddDays(3)),
            DateOnly.FromDateTime(DateTime.Today),
            "Lý do hợp lệ");

        Func<Task> act = () => handler.HandleAsync(Guid.NewGuid(), req);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>
    /// Sau khi lưu đơn nghỉ phép, handler phải truy vấn danh sách Owner để gửi thông báo —
    /// nếu không gọi thì Owner sẽ không biết có đơn mới cần duyệt.
    /// </summary>
    [Test]
    public async Task HandleAsync_ValidRequest_NotifiesOwnerRole()
    {
        var user = User.Create("emp", "emp@test.com", "hash", "Staff", null, "Nhân Viên Test");
        _repo.When(r => r.AddAsync(Arg.Any<LeaveRequest>(), Arg.Any<CancellationToken>()))
            .Do(call => typeof(LeaveRequest).GetProperty("User")!.SetValue(call.Arg<LeaveRequest>(), user));
        _userRepo.GetUserIdsByRoleAsync("Owner", Arg.Any<CancellationToken>()).Returns([]);
        var handler = new CreateLeaveRequestHandler(_repo, _activityLog, _notification, _userRepo, _currentUser);

        await handler.HandleAsync(Guid.NewGuid(), BuildRequest("Annual", "Lý do hợp lệ"));

        await _userRepo.Received(1).GetUserIdsByRoleAsync("Owner", Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Khi có 2 owner, CreateForMultipleUsersAsync phải được gọi đúng 1 lần với đầy đủ 2 ID —
    /// gọi nhiều lần hoặc thiếu ID sẽ dẫn đến owner không nhận hoặc nhận thông báo trùng.
    /// </summary>
    [Test]
    public async Task HandleAsync_ValidRequest_SendsNotificationToEachOwner()
    {
        var user = User.Create("emp", "emp@test.com", "hash", "Staff", null, "Nhân Viên Test");
        _repo.When(r => r.AddAsync(Arg.Any<LeaveRequest>(), Arg.Any<CancellationToken>()))
            .Do(call => typeof(LeaveRequest).GetProperty("User")!.SetValue(call.Arg<LeaveRequest>(), user));
        var owner1 = Guid.NewGuid();
        var owner2 = Guid.NewGuid();
        _userRepo.GetUserIdsByRoleAsync("Owner", Arg.Any<CancellationToken>())
            .Returns(new List<Guid> { owner1, owner2 });
        var handler = new CreateLeaveRequestHandler(_repo, _activityLog, _notification, _userRepo, _currentUser);

        await handler.HandleAsync(Guid.NewGuid(), BuildRequest("Annual", "Lý do hợp lệ"));

        await _notification.Received(1).CreateForMultipleUsersAsync(
            Arg.Is<IEnumerable<Guid>>(ids => ids.Contains(owner1) && ids.Contains(owner2)),
            Arg.Any<CreateNotificationRequest>(),
            Arg.Any<CancellationToken>());
    }

    private static CreateLeaveRequestRequest BuildRequest(string leaveType, string reason)
        => new(leaveType,
            DateOnly.FromDateTime(DateTime.Today),
            DateOnly.FromDateTime(DateTime.Today.AddDays(2)),
            reason);
}
