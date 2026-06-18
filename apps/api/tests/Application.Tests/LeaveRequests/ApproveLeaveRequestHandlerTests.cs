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
public class ApproveLeaveRequestHandlerTests
{
    private ILeaveRequestRepository _repo = null!;

    [SetUp]
    public void SetUp() => _repo = Substitute.For<ILeaveRequestRepository>();

    /// <summary>
    /// Duyệt đơn đang Pending phải gọi UpdateAsync và trả về DTO với status Approved.
    /// </summary>
    [Test]
    public async Task HandleAsync_PendingRequest_ApprovesAndReturnsApprovedStatus()
    {
        var lr = MakeRequest();
        _repo.GetByIdAsync(lr.Id, Arg.Any<CancellationToken>()).Returns(lr);
        var handler = new ApproveLeaveRequestHandler(_repo);

        var result = await handler.HandleAsync(lr.Id);

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
        var handler = new ApproveLeaveRequestHandler(_repo);

        Func<Task> act = () => handler.HandleAsync(Guid.NewGuid());

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
        var handler = new ApproveLeaveRequestHandler(_repo);

        Func<Task> act = () => handler.HandleAsync(lr.Id);

        await act.Should().ThrowAsync<ValidationException>();
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
