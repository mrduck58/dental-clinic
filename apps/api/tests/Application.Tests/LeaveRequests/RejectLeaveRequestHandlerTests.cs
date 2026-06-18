using DentalClinic.API.Application.DTOs.LeaveRequests;
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
public class RejectLeaveRequestHandlerTests
{
    private ILeaveRequestRepository _repo = null!;

    [SetUp]
    public void SetUp() => _repo = Substitute.For<ILeaveRequestRepository>();

    /// <summary>
    /// Từ chối đơn Pending phải lưu reviewer note và trả về status Rejected.
    /// </summary>
    [Test]
    public async Task HandleAsync_PendingRequest_RejectsWithReviewerNote()
    {
        var lr = MakeRequest();
        _repo.GetByIdAsync(lr.Id, Arg.Any<CancellationToken>()).Returns(lr);
        var handler = new RejectLeaveRequestHandler(_repo);

        var result = await handler.HandleAsync(lr.Id, new RejectLeaveRequestRequest("Không đủ điều kiện"));

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
        var handler = new RejectLeaveRequestHandler(_repo);

        Func<Task> act = () => handler.HandleAsync(Guid.NewGuid(), new RejectLeaveRequestRequest(null));

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
        var handler = new RejectLeaveRequestHandler(_repo);

        Func<Task> act = () => handler.HandleAsync(lr.Id, new RejectLeaveRequestRequest(null));

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
