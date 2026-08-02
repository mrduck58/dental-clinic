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

        var result = await handler.Handle(new GetLeaveRequestsQuery(null, null), CancellationToken.None);

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

        var result = await handler.Handle(new GetLeaveRequestsQuery("Pending", null), CancellationToken.None);

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

        var result = await handler.Handle(new GetLeaveRequestsQuery("Approved", null), CancellationToken.None);

        result.Should().BeEmpty();
    }

    /// <summary>
    /// Status chỉ toàn khoảng trắng phải được xem như không có filter (IsNullOrWhiteSpace),
    /// trả về toàn bộ danh sách thay vì không khớp gì.
    /// </summary>
    [Test]
    public async Task HandleAsync_WhitespaceStatus_ReturnsAll()
    {
        _repo.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<LeaveRequest> { MakeRequest(), MakeRequest() });
        var handler = new GetLeaveRequestsHandler(_repo);

        var result = await handler.Handle(new GetLeaveRequestsQuery("   ", null), CancellationToken.None);

        result.Should().HaveCount(2);
    }

    /// <summary>
    /// Search khớp theo tên nhân viên (không phân biệt hoa thường) phải trả về đúng đơn.
    /// </summary>
    [Test]
    public async Task HandleAsync_SearchByUserFullName_ReturnsMatchingRequest()
    {
        var match = MakeRequest(fullName: "Nguyễn Văn A");
        var other = MakeRequest(fullName: "Trần Thị B");
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<LeaveRequest> { match, other });
        var handler = new GetLeaveRequestsHandler(_repo);

        var result = await handler.Handle(new GetLeaveRequestsQuery(null, "văn a"), CancellationToken.None);

        result.Should().ContainSingle(r => r.Id == match.Id);
    }

    /// <summary>
    /// Search khớp theo nội dung lý do nghỉ phép phải trả về đúng đơn,
    /// ngay cả khi tên nhân viên không khớp.
    /// </summary>
    [Test]
    public async Task HandleAsync_SearchByReason_ReturnsMatchingRequest()
    {
        var match = MakeRequest(reason: "Khám sức khỏe định kỳ");
        var other = MakeRequest(reason: "Việc gia đình");
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<LeaveRequest> { match, other });
        var handler = new GetLeaveRequestsHandler(_repo);

        var result = await handler.Handle(new GetLeaveRequestsQuery(null, "khám sức khỏe"), CancellationToken.None);

        result.Should().ContainSingle(r => r.Id == match.Id);
    }

    /// <summary>
    /// Kết hợp filter status và search cùng lúc phải áp dụng cả hai điều kiện,
    /// chỉ trả về đơn thỏa mãn đồng thời cả hai.
    /// </summary>
    [Test]
    public async Task HandleAsync_CombinedStatusAndSearch_AppliesBothFilters()
    {
        var matchBoth = MakeRequest(reason: "Nghỉ ốm");
        var wrongStatus = MakeRequest(reason: "Nghỉ ốm");
        wrongStatus.Approve();
        var wrongSearch = MakeRequest(reason: "Việc riêng");
        _repo.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<LeaveRequest> { matchBoth, wrongStatus, wrongSearch });
        var handler = new GetLeaveRequestsHandler(_repo);

        var result = await handler.Handle(new GetLeaveRequestsQuery("Pending", "ốm"), CancellationToken.None);

        result.Should().ContainSingle(r => r.Id == matchBoth.Id);
    }

    /// <summary>
    /// Khi User.FullName là null, tên hiển thị trong DTO phải fallback về Email
    /// thay vì trả về chuỗi rỗng hoặc null.
    /// </summary>
    [Test]
    public async Task HandleAsync_UserFullNameNull_DtoFallsBackToEmail()
    {
        var lr = MakeRequest(fullName: null);
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<LeaveRequest> { lr });
        var handler = new GetLeaveRequestsHandler(_repo);

        var result = await handler.Handle(new GetLeaveRequestsQuery(null, null), CancellationToken.None);

        result.Single().UserFullName.Should().Be(lr.User.Email);
    }

    private static LeaveRequest MakeRequest(string? fullName = "Test", string reason = "Lý do test")
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var lr = LeaveRequest.Create(Guid.NewGuid(), LeaveType.Annual, today, today.AddDays(2), reason);
        var user = User.Create("emp", "emp@test.com", "hash", "Staff", null, fullName);
        typeof(LeaveRequest).GetProperty("User")!.SetValue(lr, user);
        return lr;
    }
}
