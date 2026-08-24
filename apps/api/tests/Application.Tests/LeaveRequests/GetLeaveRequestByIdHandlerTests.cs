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
public class GetLeaveRequestByIdHandlerTests
{
    private ILeaveRequestRepository _repo = null!;

    [SetUp]
    public void SetUp() => _repo = Substitute.For<ILeaveRequestRepository>();

    /// <summary>
    /// Chính chủ lấy đơn của mình phải trả về đúng DTO.
    /// </summary>
    [Test]
    public async Task HandleAsync_OwnRequest_ReturnsDto()
    {
        var lr = MakeRequest();
        _repo.GetByIdAsync(lr.Id, Arg.Any<CancellationToken>()).Returns(lr);
        var handler = new GetLeaveRequestByIdHandler(_repo);

        var result = await handler.Handle(
            new GetLeaveRequestByIdQuery(lr.Id, lr.UserId, CanViewAll: false), CancellationToken.None);

        result.Id.Should().Be(lr.Id);
    }

    /// <summary>
    /// Người duyệt đơn (CanViewAll) đọc được đơn của người khác.
    /// </summary>
    [Test]
    public async Task HandleAsync_CanViewAll_ReturnsOtherUsersRequest()
    {
        var lr = MakeRequest();
        _repo.GetByIdAsync(lr.Id, Arg.Any<CancellationToken>()).Returns(lr);
        var handler = new GetLeaveRequestByIdHandler(_repo);

        var result = await handler.Handle(
            new GetLeaveRequestByIdQuery(lr.Id, Guid.NewGuid(), CanViewAll: true), CancellationToken.None);

        result.Id.Should().Be(lr.Id);
    }

    /// <summary>
    /// Đơn của người khác phải trả 404 y như đơn không tồn tại — không được xác nhận id có thật.
    /// </summary>
    [Test]
    public async Task HandleAsync_OtherUsersRequest_ThrowsNotFoundException()
    {
        var lr = MakeRequest();
        _repo.GetByIdAsync(lr.Id, Arg.Any<CancellationToken>()).Returns(lr);
        var handler = new GetLeaveRequestByIdHandler(_repo);

        Func<Task> act = () => handler.Handle(
            new GetLeaveRequestByIdQuery(lr.Id, Guid.NewGuid(), CanViewAll: false), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>
    /// ID không tồn tại phải ném NotFoundException.
    /// </summary>
    [Test]
    public async Task HandleAsync_NotFound_ThrowsNotFoundException()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((LeaveRequest?)null);
        var handler = new GetLeaveRequestByIdHandler(_repo);

        Func<Task> act = () => handler.Handle(
            new GetLeaveRequestByIdQuery(Guid.NewGuid(), Guid.NewGuid(), CanViewAll: true), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    private static LeaveRequest MakeRequest()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var shifts = new List<(DateOnly Date, string ShiftId)>
        {
            (today, "08:00-10:00"),
            (today.AddDays(1), "08:00-10:00"),
            (today.AddDays(2), "08:00-10:00"),
        };
        var lr = LeaveRequest.Create(Guid.NewGuid(), LeaveType.Annual, shifts, "Lý do test");
        var user = User.Create("emp", "emp@test.com", "hash", UserRole.Staff, null, "Test");
        typeof(LeaveRequest).GetProperty("User")!.SetValue(lr, user);
        return lr;
    }
}
