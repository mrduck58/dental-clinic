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
public class CancelLeaveRequestHandlerTests
{
    private ILeaveRequestRepository _repo = null!;

    [SetUp]
    public void SetUp() => _repo = Substitute.For<ILeaveRequestRepository>();

    /// <summary>
    /// Chính chủ hủy đơn Pending của mình phải thành công và trả về status Cancelled.
    /// </summary>
    [Test]
    public async Task HandleAsync_OwnerCancelsPendingRequest_ReturnsCancelled()
    {
        var userId = Guid.NewGuid();
        var lr = MakeRequest(userId);
        _repo.GetByIdAsync(lr.Id, Arg.Any<CancellationToken>()).Returns(lr);
        var handler = new CancelLeaveRequestHandler(_repo);

        var result = await handler.HandleAsync(lr.Id, userId);

        result.Status.Should().Be("Cancelled");
    }

    /// <summary>
    /// Hủy đơn không tồn tại phải ném NotFoundException.
    /// </summary>
    [Test]
    public async Task HandleAsync_NotFound_ThrowsNotFoundException()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((LeaveRequest?)null);
        var handler = new CancelLeaveRequestHandler(_repo);

        Func<Task> act = () => handler.HandleAsync(Guid.NewGuid(), Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>
    /// User khác cố hủy đơn không phải của mình phải ném ValidationException,
    /// bảo vệ quyền sở hữu đơn nghỉ phép.
    /// </summary>
    [Test]
    public async Task HandleAsync_DifferentUser_ThrowsValidationException()
    {
        var lr = MakeRequest(Guid.NewGuid());
        _repo.GetByIdAsync(lr.Id, Arg.Any<CancellationToken>()).Returns(lr);
        var handler = new CancelLeaveRequestHandler(_repo);

        Func<Task> act = () => handler.HandleAsync(lr.Id, Guid.NewGuid());

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>
    /// Hủy đơn đã được duyệt (Approved) phải ném ValidationException,
    /// không cho hủy đơn đã xử lý xong.
    /// </summary>
    [Test]
    public async Task HandleAsync_ApprovedRequest_ThrowsValidationException()
    {
        var userId = Guid.NewGuid();
        var lr = MakeRequest(userId);
        lr.Approve();
        _repo.GetByIdAsync(lr.Id, Arg.Any<CancellationToken>()).Returns(lr);
        var handler = new CancelLeaveRequestHandler(_repo);

        Func<Task> act = () => handler.HandleAsync(lr.Id, userId);

        await act.Should().ThrowAsync<ValidationException>();
    }

    private static LeaveRequest MakeRequest(Guid userId)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var lr = LeaveRequest.Create(userId, LeaveType.Annual, today, today.AddDays(2), "Lý do test");
        var user = User.Create("emp", "emp@test.com", "hash", "Staff", null, "Test");
        typeof(LeaveRequest).GetProperty("User")!.SetValue(lr, user);
        return lr;
    }
}
