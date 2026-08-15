using DentalClinic.API.Application.UseCases.LeaveRequests;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.LeaveRequests;

[TestFixture]
public class GetLeaveRequestImpactHandlerTests
{
    private const string StaffName = "Trần Thị Hoa";

    private ILeaveRequestRepository _repo = null!;
    private IWorkScheduleRepository _workSchedules = null!;
    private IAppointmentRepository _appointments = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = Substitute.For<ILeaveRequestRepository>();
        _workSchedules = Substitute.For<IWorkScheduleRepository>();
        _appointments = Substitute.For<IAppointmentRepository>();

        _workSchedules
            .GetByDateRangeAsync(Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new List<WorkSchedule>());
    }

    private GetLeaveRequestImpactHandler MakeHandler() => new(_repo, _workSchedules, _appointments);

    /// <summary>
    /// Xem ảnh hưởng của đơn không tồn tại phải ném NotFoundException.
    /// </summary>
    [Test]
    public async Task HandleAsync_NotFound_ThrowsNotFoundException()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((LeaveRequest?)null);

        Func<Task> act = () => MakeHandler().Handle(new GetLeaveRequestImpactQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>
    /// Các ca trùng khoảng nghỉ phải được gom theo NGÀY, mỗi ngày liệt kê đủ số ca —
    /// đây là thứ Owner nhìn thấy trước khi bấm duyệt.
    /// </summary>
    [Test]
    public async Task HandleAsync_ShiftsInRange_GroupsThemByDay()
    {
        var lr = MakeRequest(new DateOnly(2026, 8, 19), new DateOnly(2026, 8, 21));
        _repo.GetByIdAsync(lr.Id, Arg.Any<CancellationToken>()).Returns(lr);
        StubShifts(
            MakeShift(new DateOnly(2026, 8, 19), "08:00-10:00"),
            MakeShift(new DateOnly(2026, 8, 19), "10:00-12:00"),
            MakeShift(new DateOnly(2026, 8, 21), "08:00-10:00"));

        var result = await MakeHandler().Handle(new GetLeaveRequestImpactQuery(lr.Id), CancellationToken.None);

        result.AffectedShiftCount.Should().Be(3);
        result.AffectedDayCount.Should().Be(2);
        result.Days.Should().HaveCount(2);
        result.Days[0].Date.Should().Be(new DateOnly(2026, 8, 19));
        result.Days[0].Shifts.Should().HaveCount(2);
        result.Days[1].Shifts.Should().HaveCount(1);
    }

    /// <summary>
    /// Dấu nghỉ lễ của cả phòng khám không phải ca của người xin nghỉ nên không được tính là ảnh hưởng.
    /// </summary>
    [Test]
    public async Task HandleAsync_HolidayMarker_IsExcluded()
    {
        var lr = MakeRequest(new DateOnly(2026, 8, 19), new DateOnly(2026, 8, 21));
        _repo.GetByIdAsync(lr.Id, Arg.Any<CancellationToken>()).Returns(lr);
        StubShifts(MakeShift(new DateOnly(2026, 8, 20), "08:00-10:00", isHoliday: true));

        var result = await MakeHandler().Handle(new GetLeaveRequestImpactQuery(lr.Id), CancellationToken.None);

        result.AffectedShiftCount.Should().Be(0);
        result.Days.Should().BeEmpty();
    }

    /// <summary>
    /// Người nộp đơn không phải bác sĩ (không có DentistProfile) thì không có lịch hẹn nào để cảnh báo —
    /// và cũng không được đi hỏi repository lịch hẹn.
    /// </summary>
    [Test]
    public async Task HandleAsync_NonDentist_SkipsAppointmentLookup()
    {
        var lr = MakeRequest(new DateOnly(2026, 8, 19), new DateOnly(2026, 8, 21));
        _repo.GetByIdAsync(lr.Id, Arg.Any<CancellationToken>()).Returns(lr);

        var result = await MakeHandler().Handle(new GetLeaveRequestImpactQuery(lr.Id), CancellationToken.None);

        result.AffectedAppointmentCount.Should().Be(0);
        await _appointments.DidNotReceive().GetActiveByDentistIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Dữ liệu thật ghi tên rất tuỳ hứng: hồ sơ tài khoản là "BS.Đào Tuấn Anh" còn ô lịch là
    /// "Đào Tuấn Anh" hoặc "BS. Đào Tuấn Anh". Cả ba phải được hiểu là một người, nếu không Owner
    /// thấy "0 ca trùng lịch" trong khi lịch làm việc vẫn kín ca của bác sĩ đó.
    /// </summary>
    [Test]
    public async Task HandleAsync_TitlePrefixWrittenDifferently_StillMatchesSamePerson()
    {
        var lr = MakeRequest(new DateOnly(2026, 8, 15), new DateOnly(2026, 8, 16), fullName: "BS.Đào Tuấn Anh");
        _repo.GetByIdAsync(lr.Id, Arg.Any<CancellationToken>()).Returns(lr);
        StubShifts(
            MakeShift(new DateOnly(2026, 8, 15), "08:00-10:00", staffName: "Đào Tuấn Anh"),
            MakeShift(new DateOnly(2026, 8, 15), "10:00-12:00", staffName: "BS. Đào Tuấn Anh"),
            MakeShift(new DateOnly(2026, 8, 16), "08:00-10:00", staffName: "  đào   tuấn anh "));

        var result = await MakeHandler().Handle(new GetLeaveRequestImpactQuery(lr.Id), CancellationToken.None);

        result.AffectedShiftCount.Should().Be(3);
        result.AffectedDayCount.Should().Be(2);
    }

    /// <summary>
    /// Chuẩn hoá tên không được nới lỏng tới mức gộp nhầm người khác — ca của bác sĩ khác trong cùng
    /// khoảng ngày phải giữ nguyên, kể cả khi tên cũng có tiền tố chức danh.
    /// </summary>
    [Test]
    public async Task HandleAsync_OtherStaffShifts_AreNotCounted()
    {
        var lr = MakeRequest(new DateOnly(2026, 8, 15), new DateOnly(2026, 8, 16), fullName: "BS.Đào Tuấn Anh");
        _repo.GetByIdAsync(lr.Id, Arg.Any<CancellationToken>()).Returns(lr);
        StubShifts(
            MakeShift(new DateOnly(2026, 8, 15), "08:00-10:00", staffName: "Đào Tuấn Anh"),
            MakeShift(new DateOnly(2026, 8, 15), "08:00-10:00", staffName: "BS. Nguyễn Thu Thảo"),
            MakeShift(new DateOnly(2026, 8, 15), "10:00-12:00", staffName: "Đỗ Văn Phong"));

        var result = await MakeHandler().Handle(new GetLeaveRequestImpactQuery(lr.Id), CancellationToken.None);

        result.AffectedShiftCount.Should().Be(1);
        result.Days[0].Shifts.Should().HaveCount(1);
    }

    private void StubShifts(params WorkSchedule[] shifts) =>
        _workSchedules
            .GetByDateRangeAsync(Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(shifts.ToList());

    private static WorkSchedule MakeShift(DateOnly date, string shift, bool isHoliday = false, string? staffName = null) =>
        WorkSchedule.Create(date, shift, "dentist", "dentist", staffName ?? StaffName, "Phòng 1", "border-primary", isHoliday);

    private static LeaveRequest MakeRequest(DateOnly start, DateOnly end, string? fullName = null)
    {
        var lr = LeaveRequest.Create(Guid.NewGuid(), LeaveType.Annual, start, end, "Lý do test");
        var user = User.Create("emp", "emp@test.com", "hash", UserRole.Dentist, null, fullName ?? StaffName);
        typeof(LeaveRequest).GetProperty("User")!.SetValue(lr, user);
        return lr;
    }
}
