using DentalClinic.API.Application.DTOs.LeaveRequests;
using DentalClinic.API.Application.UseCases.LeaveRequests;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.LeaveRequests;

[TestFixture]
public class CreateLeaveRequestHandlerTests
{
    private ILeaveRequestRepository _repo = null!;

    [SetUp]
    public void SetUp() => _repo = Substitute.For<ILeaveRequestRepository>();

    /// <summary>
    /// Tạo đơn nghỉ phép hợp lệ phải gọi AddAsync 1 lần và trả về DTO với status Pending.
    /// </summary>
    [Test]
    public async Task HandleAsync_ValidRequest_CallsAddAsyncAndReturnsPending()
    {
        var user = User.Create("emp", "emp@test.com", "hash", "Staff", null, "Nhân Viên Test");
        _repo.When(r => r.AddAsync(Arg.Any<LeaveRequest>(), Arg.Any<CancellationToken>()))
            .Do(call => typeof(LeaveRequest).GetProperty("User")!.SetValue(call.Arg<LeaveRequest>(), user));
        var handler = new CreateLeaveRequestHandler(_repo);

        var result = await handler.HandleAsync(Guid.NewGuid(), BuildRequest("Annual", "Lý do hợp lệ"));

        await _repo.Received(1).AddAsync(Arg.Any<LeaveRequest>(), Arg.Any<CancellationToken>());
        result.Status.Should().Be("Pending");
    }

    /// <summary>
    /// Lý do nghỉ phép để trống phải ném ValidationException trước khi gọi AddAsync.
    /// </summary>
    [Test]
    public async Task HandleAsync_EmptyReason_ThrowsValidationException()
    {
        var handler = new CreateLeaveRequestHandler(_repo);

        Func<Task> act = () => handler.HandleAsync(Guid.NewGuid(), BuildRequest("Annual", ""));

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>
    /// Lý do nghỉ phép vượt 1000 ký tự phải ném ValidationException.
    /// </summary>
    [Test]
    public async Task HandleAsync_ReasonOver1000Chars_ThrowsValidationException()
    {
        var handler = new CreateLeaveRequestHandler(_repo);

        Func<Task> act = () => handler.HandleAsync(Guid.NewGuid(), BuildRequest("Annual", new string('a', 1001)));

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>
    /// Loại nghỉ phép không hợp lệ (không nằm trong enum) phải ném ValidationException.
    /// </summary>
    [Test]
    public async Task HandleAsync_InvalidLeaveType_ThrowsValidationException()
    {
        var handler = new CreateLeaveRequestHandler(_repo);

        Func<Task> act = () => handler.HandleAsync(Guid.NewGuid(), BuildRequest("InvalidType", "Lý do"));

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>
    /// Ngày kết thúc trước ngày bắt đầu phải ném ValidationException từ entity.
    /// </summary>
    [Test]
    public async Task HandleAsync_EndDateBeforeStartDate_ThrowsValidationException()
    {
        var handler = new CreateLeaveRequestHandler(_repo);
        var req = new CreateLeaveRequestRequest(
            "Annual",
            DateOnly.FromDateTime(DateTime.Today.AddDays(3)),
            DateOnly.FromDateTime(DateTime.Today),
            "Lý do hợp lệ");

        Func<Task> act = () => handler.HandleAsync(Guid.NewGuid(), req);

        await act.Should().ThrowAsync<ValidationException>();
    }

    private static CreateLeaveRequestRequest BuildRequest(string leaveType, string reason)
        => new(leaveType,
            DateOnly.FromDateTime(DateTime.Today),
            DateOnly.FromDateTime(DateTime.Today.AddDays(2)),
            reason);
}
