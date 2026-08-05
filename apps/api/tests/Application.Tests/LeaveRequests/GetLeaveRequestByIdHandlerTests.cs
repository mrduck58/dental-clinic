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
    /// Lấy đơn theo ID tồn tại phải trả về đúng DTO.
    /// </summary>
    [Test]
    public async Task HandleAsync_ExistingRequest_ReturnsDto()
    {
        var lr = MakeRequest();
        _repo.GetByIdAsync(lr.Id, Arg.Any<CancellationToken>()).Returns(lr);
        var handler = new GetLeaveRequestByIdHandler(_repo);

        var result = await handler.Handle(new GetLeaveRequestByIdQuery(lr.Id), CancellationToken.None);

        result.Id.Should().Be(lr.Id);
    }

    /// <summary>
    /// ID không tồn tại phải ném NotFoundException.
    /// </summary>
    [Test]
    public async Task HandleAsync_NotFound_ThrowsNotFoundException()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((LeaveRequest?)null);
        var handler = new GetLeaveRequestByIdHandler(_repo);

        Func<Task> act = () => handler.Handle(new GetLeaveRequestByIdQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    private static LeaveRequest MakeRequest()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var lr = LeaveRequest.Create(Guid.NewGuid(), LeaveType.Annual, today, today.AddDays(2), "Lý do test");
        var user = User.Create("emp", "emp@test.com", "hash", UserRole.Staff, null, "Test");
        typeof(LeaveRequest).GetProperty("User")!.SetValue(lr, user);
        return lr;
    }
}
