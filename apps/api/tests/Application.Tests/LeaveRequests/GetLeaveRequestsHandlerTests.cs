using DentalClinic.API.Application.UseCases.LeaveRequests;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Interfaces.Repositories;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.LeaveRequests;

[TestFixture]
public class GetLeaveRequestsHandlerTests
{
    private ILeaveRequestRepository _repo = null!;

    [SetUp]
    public void SetUp() => _repo = Substitute.For<ILeaveRequestRepository>();

    /// <summary>
    /// Không có filter trả về toàn bộ danh sách đơn nghỉ phép.
    /// </summary>
    [Test]
    public async Task HandleAsync_NoFilters_ReturnsAll()
    {
        _repo.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<LeaveRequest> { MakeRequest(), MakeRequest(), MakeRequest() });
        var handler = new GetLeaveRequestsHandler(_repo);

        var result = await handler.HandleAsync(null, null);

        result.Should().HaveCount(3);
    }

    /// <summary>
    /// Filter theo status chỉ trả về đơn có đúng trạng thái (không phân biệt hoa thường).
    /// </summary>
    [Test]
    public async Task HandleAsync_FilterByStatus_ReturnsOnlyMatchingStatus()
    {
        var pending = MakeRequest();
        var approved = MakeRequest();
        approved.Approve();
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<LeaveRequest> { pending, approved });
        var handler = new GetLeaveRequestsHandler(_repo);

        var result = await handler.HandleAsync(status: "Pending", null);

        result.Should().HaveCount(1);
        result.Single().Status.Should().Be("Pending");
    }

    /// <summary>
    /// Không có đơn nào khớp filter phải trả về danh sách rỗng, không throw.
    /// </summary>
    [Test]
    public async Task HandleAsync_NoMatchingFilter_ReturnsEmpty()
    {
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<LeaveRequest> { MakeRequest() });
        var handler = new GetLeaveRequestsHandler(_repo);

        var result = await handler.HandleAsync(status: "Approved", null);

        result.Should().BeEmpty();
    }

    private static LeaveRequest MakeRequest()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var lr = LeaveRequest.Create(Guid.NewGuid(), LeaveType.Annual, today, today.AddDays(2), "Lý do test");
        var user = User.Create("emp", "emp@test.com", "hash", "Staff", null, "Test");
        typeof(LeaveRequest).GetProperty("User")!.SetValue(lr, user);
        return lr;
    }
}
