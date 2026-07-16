using DentalClinic.API.Application.UseCases.LeaveRequests;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Interfaces.Repositories;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.LeaveRequests;

[TestFixture]
public class GetMyLeaveRequestsHandlerTests
{
    private ILeaveRequestRepository _repo = null!;

    [SetUp]
    public void SetUp() => _repo = Substitute.For<ILeaveRequestRepository>();

    /// <summary>
    /// Tổng số ngày nghỉ phép năm luôn là 12, là hằng số quy định của công ty.
    /// </summary>
    [Test]
    public async Task HandleAsync_TotalAnnualDaysIs12()
    {
        var userId = Guid.NewGuid();
        _repo.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(new List<LeaveRequest>());
        var handler = new GetMyLeaveRequestsHandler(_repo);

        var result = await handler.HandleAsync(userId);

        result.Stats.TotalAnnualDays.Should().Be(12);
    }

    /// <summary>
    /// UsedAnnualDays chỉ tính đơn loại Annual, đã Approved, trong năm hiện tại.
    /// Đơn Sick hoặc chưa Approved không được tính vào.
    /// </summary>
    [Test]
    public async Task HandleAsync_UsedAnnualDays_CountsOnlyApprovedAnnualThisYear()
    {
        var userId = Guid.NewGuid();
        var approvedAnnual = MakeRequest(userId, LeaveType.Annual, daysCount: 3);
        approvedAnnual.Approve();
        var pendingAnnual = MakeRequest(userId, LeaveType.Annual, daysCount: 2);
        var approvedSick = MakeRequest(userId, LeaveType.Sick, daysCount: 1);
        approvedSick.Approve();

        _repo.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new List<LeaveRequest> { approvedAnnual, pendingAnnual, approvedSick });
        var handler = new GetMyLeaveRequestsHandler(_repo);

        var result = await handler.HandleAsync(userId);

        result.Stats.UsedAnnualDays.Should().Be(3);
    }

    /// <summary>
    /// PendingCount phải đếm đúng số đơn đang chờ xử lý của user.
    /// </summary>
    [Test]
    public async Task HandleAsync_PendingCount_CountsOnlyPendingRequests()
    {
        var userId = Guid.NewGuid();
        var pending1 = MakeRequest(userId);
        var pending2 = MakeRequest(userId);
        var approved = MakeRequest(userId);
        approved.Approve();

        _repo.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new List<LeaveRequest> { pending1, pending2, approved });
        var handler = new GetMyLeaveRequestsHandler(_repo);

        var result = await handler.HandleAsync(userId);

        result.Stats.PendingCount.Should().Be(2);
    }

    /// <summary>
    /// RemainingAnnualDays phải bằng TotalAnnualDays trừ đi UsedAnnualDays.
    /// </summary>
    [Test]
    public async Task HandleAsync_RemainingAnnualDays_EqualsTotalMinusUsed()
    {
        var userId = Guid.NewGuid();
        var approvedAnnual = MakeRequest(userId, LeaveType.Annual, daysCount: 5);
        approvedAnnual.Approve();

        _repo.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new List<LeaveRequest> { approvedAnnual });
        var handler = new GetMyLeaveRequestsHandler(_repo);

        var result = await handler.HandleAsync(userId);

        result.Stats.RemainingAnnualDays.Should().Be(7);
    }

    /// <summary>
    /// ApprovedThisYear phải đếm mọi đơn Approved trong năm hiện tại bất kể loại nghỉ phép
    /// (khác với UsedAnnualDays chỉ tính riêng loại Annual).
    /// </summary>
    [Test]
    public async Task HandleAsync_ApprovedThisYear_CountsApprovedRegardlessOfLeaveType()
    {
        var userId = Guid.NewGuid();
        var approvedAnnual = MakeRequest(userId, LeaveType.Annual);
        approvedAnnual.Approve();
        var approvedSick = MakeRequest(userId, LeaveType.Sick);
        approvedSick.Approve();
        var pendingAnnual = MakeRequest(userId, LeaveType.Annual);

        _repo.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new List<LeaveRequest> { approvedAnnual, approvedSick, pendingAnnual });
        var handler = new GetMyLeaveRequestsHandler(_repo);

        var result = await handler.HandleAsync(userId);

        result.Stats.ApprovedThisYear.Should().Be(2);
    }

    /// <summary>
    /// UsedAnnualDays không được tính đơn Annual đã Approved của năm trước,
    /// dù cùng là loại Annual và cùng trạng thái Approved.
    /// </summary>
    [Test]
    public async Task HandleAsync_UsedAnnualDays_ExcludesPreviousYearApprovedRequests()
    {
        var userId = Guid.NewGuid();
        var previousYear = DateTimeOffset.UtcNow.Year - 1;
        var lastYearStart = new DateOnly(previousYear, 12, 1);
        var lastYearApproved = LeaveRequest.Create(userId, LeaveType.Annual, lastYearStart, lastYearStart.AddDays(2), "Lý do test");
        lastYearApproved.Approve();
        var thisYearApproved = MakeRequest(userId, LeaveType.Annual, daysCount: 4);
        thisYearApproved.Approve();

        _repo.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new List<LeaveRequest> { lastYearApproved, thisYearApproved });
        var handler = new GetMyLeaveRequestsHandler(_repo);

        var result = await handler.HandleAsync(userId);

        result.Stats.UsedAnnualDays.Should().Be(4);
    }

    /// <summary>
    /// Không có đơn nghỉ phép nào phải trả về toàn bộ chỉ số 0
    /// (trừ TotalAnnualDays là hằng số) và danh sách Requests rỗng.
    /// </summary>
    [Test]
    public async Task HandleAsync_NoRequests_ReturnsZeroStatsAndEmptyList()
    {
        var userId = Guid.NewGuid();
        _repo.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(new List<LeaveRequest>());
        var handler = new GetMyLeaveRequestsHandler(_repo);

        var result = await handler.HandleAsync(userId);

        result.Stats.UsedAnnualDays.Should().Be(0);
        result.Stats.RemainingAnnualDays.Should().Be(12);
        result.Stats.PendingCount.Should().Be(0);
        result.Stats.ApprovedThisYear.Should().Be(0);
        result.Requests.Should().BeEmpty();
    }

    /// <summary>
    /// Danh sách Requests trả về phải chứa đúng số lượng đơn của user,
    /// ánh xạ đầy đủ sang DTO.
    /// </summary>
    [Test]
    public async Task HandleAsync_ReturnsAllUserRequestsMappedToDto()
    {
        var userId = Guid.NewGuid();
        var r1 = MakeRequest(userId);
        var r2 = MakeRequest(userId);
        _repo.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new List<LeaveRequest> { r1, r2 });
        var handler = new GetMyLeaveRequestsHandler(_repo);

        var result = await handler.HandleAsync(userId);

        result.Requests.Should().HaveCount(2);
        result.Requests.Select(r => r.Id).Should().BeEquivalentTo(new[] { r1.Id, r2.Id });
    }

    private static LeaveRequest MakeRequest(Guid userId, LeaveType type = LeaveType.Annual, int daysCount = 3)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var lr = LeaveRequest.Create(userId, type, today, today.AddDays(daysCount - 1), "Lý do test");
        var user = User.Create("emp", "emp@test.com", "hash", "Staff", null, "Test");
        typeof(LeaveRequest).GetProperty("User")!.SetValue(lr, user);
        return lr;
    }
}
