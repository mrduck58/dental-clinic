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

    private static LeaveRequest MakeRequest(Guid userId, LeaveType type = LeaveType.Annual, int daysCount = 3)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        return LeaveRequest.Create(userId, type, today, today.AddDays(daysCount - 1), "Lý do test");
    }
}
