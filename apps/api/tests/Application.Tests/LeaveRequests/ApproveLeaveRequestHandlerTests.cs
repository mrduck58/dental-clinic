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
    private const string StaffName = "Trần Thị Hoa";

    private ILeaveRequestRepository _repo = null!;
    private IWorkScheduleRepository _workSchedules = null!;
    private IAppointmentRepository _appointments = null!;
    private IUserRepository _users = null!;
    private IActivityLogService _activityLog = null!;
    private INotificationService _notification = null!;
    private ICurrentUserService _currentUser = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = Substitute.For<ILeaveRequestRepository>();
        _workSchedules = Substitute.For<IWorkScheduleRepository>();
        _appointments = Substitute.For<IAppointmentRepository>();
        _users = Substitute.For<IUserRepository>();
        _activityLog = Substitute.For<IActivityLogService>();
        _notification = Substitute.For<INotificationService>();
        _currentUser = Substitute.For<ICurrentUserService>();

        _workSchedules
            .GetByDateRangeAsync(Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new List<WorkSchedule>());
        _users.GetUserIdsByRoleAsync("Owner", Arg.Any<CancellationToken>())
            .Returns(new List<Guid> { Guid.NewGuid() });
    }

    private ApproveLeaveRequestHandler MakeHandler() => new(
        _repo, _workSchedules, _appointments, _users, _activityLog, _notification, _currentUser);

    /// <summary>
    /// Duyệt đơn đang Pending phải gọi UpdateAsync và trả về DTO với status Approved.
    /// </summary>
    [Test]
    public async Task HandleAsync_PendingRequest_ApprovesAndReturnsApprovedStatus()
    {
        var lr = MakeRequest();
        _repo.GetByIdAsync(lr.Id, Arg.Any<CancellationToken>()).Returns(lr);
        var handler = MakeHandler();

        var result = await handler.Handle(new ApproveLeaveRequestCommand(lr.Id), CancellationToken.None);

        await _repo.Received(1).UpdateAsync(lr, Arg.Any<CancellationToken>());
        result.Request.Status.Should().Be("Approved");
    }

    /// <summary>
    /// Duyệt đơn không tồn tại phải ném NotFoundException.
    /// </summary>
    [Test]
    public async Task HandleAsync_NotFound_ThrowsNotFoundException()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((LeaveRequest?)null);
        var handler = MakeHandler();

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
        var handler = MakeHandler();

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
        var handler = MakeHandler();

        Func<Task> act = () => handler.Handle(new ApproveLeaveRequestCommand(lr.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>
    /// Đơn đã xử lý bị từ chối duyệt lại thì lịch làm việc phải còn NGUYÊN — không được xóa ca nào,
    /// vì thao tác duyệt không hề diễn ra.
    /// </summary>
    [Test]
    public async Task HandleAsync_AlreadyApproved_DoesNotRemoveAnyShift()
    {
        var lr = MakeRequest();
        lr.Approve();
        _repo.GetByIdAsync(lr.Id, Arg.Any<CancellationToken>()).Returns(lr);
        StubShifts(MakeShift(lr.StartDate));
        var handler = MakeHandler();

        Func<Task> act = () => handler.Handle(new ApproveLeaveRequestCommand(lr.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        await _workSchedules.DidNotReceive().RemoveRangeAsync(Arg.Any<IEnumerable<WorkSchedule>>(), Arg.Any<CancellationToken>());
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
        var handler = MakeHandler();

        await handler.Handle(new ApproveLeaveRequestCommand(lr.Id), CancellationToken.None);

        await _notification.Received(1).CreateAsync(
            Arg.Is<CreateNotificationRequest>(n => n.UserId == lr.UserId),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Các ca đã xếp cho người xin nghỉ trong khoảng nghỉ phải bị gỡ khỏi lịch làm việc —
    /// người được duyệt nghỉ mà vẫn đứng tên trong lịch thì lễ tân vẫn xếp bệnh nhân vào phòng đó.
    /// </summary>
    [Test]
    public async Task HandleAsync_ShiftsInLeaveRange_RemovesThemFromWorkSchedule()
    {
        var lr = MakeRequest();
        _repo.GetByIdAsync(lr.Id, Arg.Any<CancellationToken>()).Returns(lr);
        var shifts = new[] { MakeShift(lr.StartDate), MakeShift(lr.StartDate.AddDays(1)) };
        StubShifts(shifts);
        var handler = MakeHandler();

        var result = await handler.Handle(new ApproveLeaveRequestCommand(lr.Id), CancellationToken.None);

        await _workSchedules.Received(1).RemoveRangeAsync(
            Arg.Is<IEnumerable<WorkSchedule>>(s => s.Count() == 2), Arg.Any<CancellationToken>());
        result.RemovedShiftCount.Should().Be(2);
        result.AffectedDayCount.Should().Be(2);
    }

    /// <summary>
    /// Bản ghi đánh dấu nghỉ lễ (IsHoliday) là của cả phòng khám, không thuộc về ai —
    /// duyệt đơn nghỉ của một người không được xóa nó.
    /// </summary>
    [Test]
    public async Task HandleAsync_HolidayMarker_IsNotRemoved()
    {
        var lr = MakeRequest();
        _repo.GetByIdAsync(lr.Id, Arg.Any<CancellationToken>()).Returns(lr);
        StubShifts(MakeShift(lr.StartDate), MakeShift(lr.StartDate.AddDays(1), isHoliday: true));
        var handler = MakeHandler();

        var result = await handler.Handle(new ApproveLeaveRequestCommand(lr.Id), CancellationToken.None);

        result.RemovedShiftCount.Should().Be(1);
        await _workSchedules.Received(1).RemoveRangeAsync(
            Arg.Is<IEnumerable<WorkSchedule>>(s => s.All(x => !x.IsHoliday)), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Gỡ ca xong phải báo lại cho Owner là lịch đang trống và cần bổ sung người,
    /// kèm link về đúng tuần chứa ca đầu tiên bị gỡ (RelatedEntityId = thứ Hai của tuần đó).
    /// </summary>
    [Test]
    public async Task HandleAsync_ShiftsRemoved_NotifiesOwnersWithWeekLink()
    {
        var lr = MakeRequest(new DateOnly(2026, 8, 19), new DateOnly(2026, 8, 20)); // thứ Tư → thứ Năm
        _repo.GetByIdAsync(lr.Id, Arg.Any<CancellationToken>()).Returns(lr);
        StubShifts(MakeShift(new DateOnly(2026, 8, 19)));
        var handler = MakeHandler();

        await handler.Handle(new ApproveLeaveRequestCommand(lr.Id), CancellationToken.None);

        await _notification.Received(1).CreateForMultipleUsersAsync(
            Arg.Any<IEnumerable<Guid>>(),
            Arg.Is<CreateNotificationRequest>(n =>
                n.RelatedEntityType == "WorkSchedule" &&
                n.RelatedEntityId == "2026-08-17"),   // thứ Hai cùng tuần
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Đơn nghỉ không trùng ca nào thì không có gì để bổ sung — không làm phiền Owner bằng thông báo.
    /// </summary>
    [Test]
    public async Task HandleAsync_NoShiftsAffected_DoesNotNotifyOwners()
    {
        var lr = MakeRequest();
        _repo.GetByIdAsync(lr.Id, Arg.Any<CancellationToken>()).Returns(lr);
        var handler = MakeHandler();

        var result = await handler.Handle(new ApproveLeaveRequestCommand(lr.Id), CancellationToken.None);

        result.RemovedShiftCount.Should().Be(0);
        await _notification.DidNotReceive().CreateForMultipleUsersAsync(
            Arg.Any<IEnumerable<Guid>>(), Arg.Any<CreateNotificationRequest>(), Arg.Any<CancellationToken>());
    }

    private void StubShifts(params WorkSchedule[] shifts) =>
        _workSchedules
            .GetByDateRangeAsync(Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(shifts.ToList());

    private static WorkSchedule MakeShift(DateOnly date, bool isHoliday = false) =>
        WorkSchedule.Create(date, "08:00-10:00", "dentist", "dentist", StaffName, "Phòng 1", "border-primary", isHoliday);

    private static LeaveRequest MakeRequest(DateOnly? start = null, DateOnly? end = null)
    {
        var from = start ?? DateOnly.FromDateTime(DateTime.Today);
        var to = end ?? from.AddDays(2);
        var lr = LeaveRequest.Create(Guid.NewGuid(), LeaveType.Annual, from, to, "Lý do test");
        var user = User.Create("emp", "emp@test.com", "hash", UserRole.Staff, null, StaffName);
        typeof(LeaveRequest).GetProperty("User")!.SetValue(lr, user);
        return lr;
    }
}
