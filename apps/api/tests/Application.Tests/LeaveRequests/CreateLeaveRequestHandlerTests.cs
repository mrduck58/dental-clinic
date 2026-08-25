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
        _repo.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new List<LeaveRequest>());
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
        var user = User.Create("emp", "emp@test.com", "hash", UserRole.Staff, null, "Nhân Viên Test");
        _repo.When(r => r.AddAsync(Arg.Any<LeaveRequest>(), Arg.Any<CancellationToken>()))
            .Do(call => typeof(LeaveRequest).GetProperty("User")!.SetValue(call.Arg<LeaveRequest>(), user));
        _userRepo.GetUserIdsByRoleAsync("Owner", Arg.Any<CancellationToken>()).Returns([]);
        var handler = new CreateLeaveRequestHandler(_repo, _activityLog, _notification, _userRepo, _currentUser);

        var result = await handler.Handle(new CreateLeaveRequestCommand(Guid.NewGuid(), BuildRequest("Annual", "Lý do hợp lệ")), CancellationToken.None);

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

        Func<Task> act = () => handler.Handle(new CreateLeaveRequestCommand(Guid.NewGuid(), BuildRequest("Annual", "")), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>
    /// Lý do nghỉ phép vượt 1000 ký tự phải ném ValidationException.
    /// </summary>
    [Test]
    public async Task HandleAsync_ReasonOver1000Chars_ThrowsValidationException()
    {
        var handler = new CreateLeaveRequestHandler(_repo, _activityLog, _notification, _userRepo, _currentUser);

        Func<Task> act = () => handler.Handle(new CreateLeaveRequestCommand(Guid.NewGuid(), BuildRequest("Annual", new string('a', 1001))), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>
    /// Loại nghỉ phép không hợp lệ (không nằm trong enum) phải ném ValidationException.
    /// </summary>
    [Test]
    public async Task HandleAsync_InvalidLeaveType_ThrowsValidationException()
    {
        var handler = new CreateLeaveRequestHandler(_repo, _activityLog, _notification, _userRepo, _currentUser);

        Func<Task> act = () => handler.Handle(new CreateLeaveRequestCommand(Guid.NewGuid(), BuildRequest("InvalidType", "Lý do")), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>
    /// Không chọn ca nào phải ném ValidationException — không còn khoảng ngày để suy ra ca.
    /// </summary>
    [Test]
    public async Task HandleAsync_NoShiftsSelected_ThrowsValidationException()
    {
        var handler = new CreateLeaveRequestHandler(_repo, _activityLog, _notification, _userRepo, _currentUser);
        var req = new CreateLeaveRequestRequest("Annual", [], "Lý do hợp lệ");

        Func<Task> act = () => handler.Handle(new CreateLeaveRequestCommand(Guid.NewGuid(), req), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>
    /// Mã ca không nằm trong danh sách ca hợp lệ của phòng khám phải ném ValidationException.
    /// </summary>
    [Test]
    public async Task HandleAsync_InvalidShiftId_ThrowsValidationException()
    {
        var handler = new CreateLeaveRequestHandler(_repo, _activityLog, _notification, _userRepo, _currentUser);
        var req = new CreateLeaveRequestRequest(
            "Annual",
            [new LeaveRequestShiftInput(DateOnly.FromDateTime(DateTime.Today), "not-a-real-shift")],
            "Lý do hợp lệ");

        Func<Task> act = () => handler.Handle(new CreateLeaveRequestCommand(Guid.NewGuid(), req), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>
    /// Ca đã nằm trong một đơn Pending của chính người này phải ném ValidationException —
    /// không cho xin nghỉ trùng ca hai lần trong khi đơn trước còn đang chờ duyệt.
    /// </summary>
    [Test]
    public async Task HandleAsync_ShiftAlreadyInPendingRequest_ThrowsValidationException()
    {
        var userId = Guid.NewGuid();
        var day = DateOnly.FromDateTime(DateTime.Today.AddDays(3));
        var existing = LeaveRequest.Create(userId, LeaveType.Annual, [(day, "08:00-10:00")], "Đơn trước");
        _repo.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(new List<LeaveRequest> { existing });
        var handler = new CreateLeaveRequestHandler(_repo, _activityLog, _notification, _userRepo, _currentUser);
        var req = new CreateLeaveRequestRequest("Annual", [new LeaveRequestShiftInput(day, "08:00-10:00")], "Đơn mới");

        Func<Task> act = () => handler.Handle(new CreateLeaveRequestCommand(userId, req), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>
    /// Ca thuộc một đơn đã bị Rejected thì không còn bị khoá — xin nghỉ lại ca đó phải thành công.
    /// </summary>
    [Test]
    public async Task HandleAsync_ShiftInRejectedRequest_IsNotBlocked()
    {
        var userId = Guid.NewGuid();
        var day = DateOnly.FromDateTime(DateTime.Today.AddDays(3));
        var rejected = LeaveRequest.Create(userId, LeaveType.Annual, [(day, "08:00-10:00")], "Đơn trước");
        rejected.Reject(null);
        _repo.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(new List<LeaveRequest> { rejected });
        var user = User.Create("emp", "emp@test.com", "hash", UserRole.Staff, null, "Nhân Viên Test");
        _repo.When(r => r.AddAsync(Arg.Any<LeaveRequest>(), Arg.Any<CancellationToken>()))
            .Do(call => typeof(LeaveRequest).GetProperty("User")!.SetValue(call.Arg<LeaveRequest>(), user));
        _userRepo.GetUserIdsByRoleAsync("Owner", Arg.Any<CancellationToken>()).Returns([]);
        var handler = new CreateLeaveRequestHandler(_repo, _activityLog, _notification, _userRepo, _currentUser);
        var req = new CreateLeaveRequestRequest("Annual", [new LeaveRequestShiftInput(day, "08:00-10:00")], "Đơn mới");

        var result = await handler.Handle(new CreateLeaveRequestCommand(userId, req), CancellationToken.None);

        result.Status.Should().Be("Pending");
    }

    /// <summary>
    /// Sau khi lưu đơn nghỉ phép, handler phải truy vấn danh sách Owner để gửi thông báo —
    /// nếu không gọi thì Owner sẽ không biết có đơn mới cần duyệt.
    /// </summary>
    [Test]
    public async Task HandleAsync_ValidRequest_NotifiesOwnerRole()
    {
        var user = User.Create("emp", "emp@test.com", "hash", UserRole.Staff, null, "Nhân Viên Test");
        _repo.When(r => r.AddAsync(Arg.Any<LeaveRequest>(), Arg.Any<CancellationToken>()))
            .Do(call => typeof(LeaveRequest).GetProperty("User")!.SetValue(call.Arg<LeaveRequest>(), user));
        _userRepo.GetUserIdsByRoleAsync("Owner", Arg.Any<CancellationToken>()).Returns([]);
        var handler = new CreateLeaveRequestHandler(_repo, _activityLog, _notification, _userRepo, _currentUser);

        await handler.Handle(new CreateLeaveRequestCommand(Guid.NewGuid(), BuildRequest("Annual", "Lý do hợp lệ")), CancellationToken.None);

        await _userRepo.Received(1).GetUserIdsByRoleAsync("Owner", Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Khi có 2 owner, CreateForMultipleUsersAsync phải được gọi đúng 1 lần với đầy đủ 2 ID —
    /// gọi nhiều lần hoặc thiếu ID sẽ dẫn đến owner không nhận hoặc nhận thông báo trùng.
    /// </summary>
    [Test]
    public async Task HandleAsync_ValidRequest_SendsNotificationToEachOwner()
    {
        var user = User.Create("emp", "emp@test.com", "hash", UserRole.Staff, null, "Nhân Viên Test");
        _repo.When(r => r.AddAsync(Arg.Any<LeaveRequest>(), Arg.Any<CancellationToken>()))
            .Do(call => typeof(LeaveRequest).GetProperty("User")!.SetValue(call.Arg<LeaveRequest>(), user));
        var owner1 = Guid.NewGuid();
        var owner2 = Guid.NewGuid();
        _userRepo.GetUserIdsByRoleAsync("Owner", Arg.Any<CancellationToken>())
            .Returns(new List<Guid> { owner1, owner2 });
        var handler = new CreateLeaveRequestHandler(_repo, _activityLog, _notification, _userRepo, _currentUser);

        await handler.Handle(new CreateLeaveRequestCommand(Guid.NewGuid(), BuildRequest("Annual", "Lý do hợp lệ")), CancellationToken.None);

        await _notification.Received(1).CreateForMultipleUsersAsync(
            Arg.Is<IEnumerable<Guid>>(ids => ids.Contains(owner1) && ids.Contains(owner2)),
            Arg.Any<CreateNotificationRequest>(),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Lý do đúng 1000 ký tự (giới hạn tối đa) phải được chấp nhận, không ném exception —
    /// kiểm tra boundary của điều kiện "vượt quá 1000 ký tự".
    /// </summary>
    [Test]
    public async Task HandleAsync_ReasonExactly1000Chars_Succeeds()
    {
        var user = User.Create("emp", "emp@test.com", "hash", UserRole.Staff, null, "Nhân Viên Test");
        _repo.When(r => r.AddAsync(Arg.Any<LeaveRequest>(), Arg.Any<CancellationToken>()))
            .Do(call => typeof(LeaveRequest).GetProperty("User")!.SetValue(call.Arg<LeaveRequest>(), user));
        _userRepo.GetUserIdsByRoleAsync("Owner", Arg.Any<CancellationToken>()).Returns([]);
        var handler = new CreateLeaveRequestHandler(_repo, _activityLog, _notification, _userRepo, _currentUser);

        var result = await handler.Handle(new CreateLeaveRequestCommand(Guid.NewGuid(), BuildRequest("Annual", new string('a', 1000))), CancellationToken.None);

        result.Status.Should().Be("Pending");
    }

    /// <summary>
    /// Lý do chỉ toàn khoảng trắng phải ném ValidationException giống như chuỗi rỗng,
    /// vì kiểm tra dùng IsNullOrWhiteSpace chứ không chỉ IsNullOrEmpty.
    /// </summary>
    [Test]
    public async Task HandleAsync_WhitespaceReason_ThrowsValidationException()
    {
        var handler = new CreateLeaveRequestHandler(_repo, _activityLog, _notification, _userRepo, _currentUser);

        Func<Task> act = () => handler.Handle(new CreateLeaveRequestCommand(Guid.NewGuid(), BuildRequest("Annual", "   ")), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>
    /// Loại nghỉ phép viết thường (không đúng case) vẫn phải parse thành công
    /// vì Enum.TryParse được gọi với ignoreCase: true.
    /// </summary>
    [Test]
    public async Task HandleAsync_LeaveTypeLowercase_ParsesSuccessfully()
    {
        var user = User.Create("emp", "emp@test.com", "hash", UserRole.Staff, null, "Nhân Viên Test");
        _repo.When(r => r.AddAsync(Arg.Any<LeaveRequest>(), Arg.Any<CancellationToken>()))
            .Do(call => typeof(LeaveRequest).GetProperty("User")!.SetValue(call.Arg<LeaveRequest>(), user));
        _userRepo.GetUserIdsByRoleAsync("Owner", Arg.Any<CancellationToken>()).Returns([]);
        var handler = new CreateLeaveRequestHandler(_repo, _activityLog, _notification, _userRepo, _currentUser);

        var result = await handler.Handle(new CreateLeaveRequestCommand(Guid.NewGuid(), BuildRequest("annual", "Lý do hợp lệ")), CancellationToken.None);

        result.LeaveType.Should().Be("Annual");
    }

    /// <summary>
    /// Chỉ chọn 1 ca duy nhất (1 ngày) là boundary hợp lệ, phải tạo được đơn với
    /// DaysCount = 1, không ném ValidationException.
    /// </summary>
    [Test]
    public async Task HandleAsync_SingleShift_CreatesSingleDayRequest()
    {
        var user = User.Create("emp", "emp@test.com", "hash", UserRole.Staff, null, "Nhân Viên Test");
        _repo.When(r => r.AddAsync(Arg.Any<LeaveRequest>(), Arg.Any<CancellationToken>()))
            .Do(call => typeof(LeaveRequest).GetProperty("User")!.SetValue(call.Arg<LeaveRequest>(), user));
        _userRepo.GetUserIdsByRoleAsync("Owner", Arg.Any<CancellationToken>()).Returns([]);
        var handler = new CreateLeaveRequestHandler(_repo, _activityLog, _notification, _userRepo, _currentUser);
        var futureDay = DateOnly.FromDateTime(DateTime.Today.AddDays(3));
        var req = new CreateLeaveRequestRequest(
            "Annual",
            [new LeaveRequestShiftInput(futureDay, "08:00-10:00")],
            "Lý do hợp lệ");

        var result = await handler.Handle(new CreateLeaveRequestCommand(Guid.NewGuid(), req), CancellationToken.None);

        result.DaysCount.Should().Be(1);
    }

    // Ngày trong tương lai (không phải "Today") để tránh test bị flaky theo giờ chạy — handler giờ
    // chặn xin nghỉ cho ca đã qua, và "hôm nay" với ca 08:00-10:00 có thể đã trôi qua lúc test chạy.
    private static CreateLeaveRequestRequest BuildRequest(string leaveType, string reason)
        => new(leaveType,
            [
                new LeaveRequestShiftInput(DateOnly.FromDateTime(DateTime.Today.AddDays(3)), "08:00-10:00"),
                new LeaveRequestShiftInput(DateOnly.FromDateTime(DateTime.Today.AddDays(5)), "10:00-12:00"),
            ],
            reason);
}
