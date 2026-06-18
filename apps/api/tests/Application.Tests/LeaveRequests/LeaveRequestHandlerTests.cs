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
public class LeaveRequestHandlerTests
{
    private ILeaveRequestRepository _repo = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = Substitute.For<ILeaveRequestRepository>();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CreateLeaveRequestHandler
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Tạo đơn nghỉ phép hợp lệ phải gọi AddAsync 1 lần và trả về DTO với status Pending.
    /// </summary>
    [Test]
    public async Task Create_ValidRequest_CallsAddAsyncAndReturnsPending()
    {
        // ToDto accesses r.User.FullName — simulate EF Core loading the navigation property
        var user = User.Create("emp", "emp@test.com", "hash", "Staff", null, "Nhân Viên Test");
        _repo.When(r => r.AddAsync(Arg.Any<LeaveRequest>(), Arg.Any<CancellationToken>()))
            .Do(call => typeof(LeaveRequest).GetProperty("User")!.SetValue(call.Arg<LeaveRequest>(), user));

        var handler = new CreateLeaveRequestHandler(_repo);
        var request = new CreateLeaveRequestRequest(
            "Annual",
            DateOnly.FromDateTime(DateTime.Today),
            DateOnly.FromDateTime(DateTime.Today.AddDays(2)),
            "Lý do nghỉ phép hợp lệ");

        var result = await handler.HandleAsync(Guid.NewGuid(), request);

        await _repo.Received(1).AddAsync(Arg.Any<LeaveRequest>(), Arg.Any<CancellationToken>());
        result.Status.Should().Be("Pending");
    }

    /// <summary>
    /// Lý do nghỉ phép để trống phải ném ValidationException,
    /// không được tạo đơn không có lý do.
    /// </summary>
    [Test]
    public async Task Create_EmptyReason_ThrowsValidationException()
    {
        var handler = new CreateLeaveRequestHandler(_repo);
        var request = new CreateLeaveRequestRequest(
            "Annual",
            DateOnly.FromDateTime(DateTime.Today),
            DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            "");

        Func<Task> act = () => handler.HandleAsync(Guid.NewGuid(), request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>
    /// Lý do nghỉ phép vượt 1000 ký tự phải ném ValidationException,
    /// giới hạn độ dài để tránh lưu dữ liệu quá lớn.
    /// </summary>
    [Test]
    public async Task Create_ReasonOver1000Chars_ThrowsValidationException()
    {
        var handler = new CreateLeaveRequestHandler(_repo);
        var request = new CreateLeaveRequestRequest(
            "Annual",
            DateOnly.FromDateTime(DateTime.Today),
            DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            new string('a', 1001));

        Func<Task> act = () => handler.HandleAsync(Guid.NewGuid(), request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>
    /// Loại nghỉ phép không hợp lệ (không nằm trong enum LeaveType) phải ném ValidationException
    /// cùng message hướng dẫn giá trị hợp lệ.
    /// </summary>
    [Test]
    public async Task Create_InvalidLeaveType_ThrowsValidationException()
    {
        var handler = new CreateLeaveRequestHandler(_repo);
        var request = new CreateLeaveRequestRequest(
            "InvalidType",
            DateOnly.FromDateTime(DateTime.Today),
            DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            "Lý do");

        Func<Task> act = () => handler.HandleAsync(Guid.NewGuid(), request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>
    /// Ngày kết thúc trước ngày bắt đầu phải ném ValidationException (thrown từ LeaveRequest.Create),
    /// không được tạo đơn với khoảng thời gian âm.
    /// </summary>
    [Test]
    public async Task Create_EndDateBeforeStartDate_ThrowsValidationException()
    {
        var handler = new CreateLeaveRequestHandler(_repo);
        var request = new CreateLeaveRequestRequest(
            "Annual",
            DateOnly.FromDateTime(DateTime.Today.AddDays(3)),
            DateOnly.FromDateTime(DateTime.Today),
            "Lý do hợp lệ");

        Func<Task> act = () => handler.HandleAsync(Guid.NewGuid(), request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ApproveLeaveRequestHandler
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Duyệt đơn đang Pending phải gọi UpdateAsync và trả về DTO với status Approved.
    /// </summary>
    [Test]
    public async Task Approve_PendingRequest_ApprovesAndReturnsApprovedStatus()
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
    public async Task Approve_NotFound_ThrowsNotFoundException()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((LeaveRequest?)null);
        var handler = new ApproveLeaveRequestHandler(_repo);

        Func<Task> act = () => handler.HandleAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>
    /// Duyệt đơn đã Approved rồi phải ném ValidationException (từ entity),
    /// không cho phép duyệt lại đơn đã xử lý.
    /// </summary>
    [Test]
    public async Task Approve_AlreadyApproved_ThrowsValidationException()
    {
        var lr = MakeRequest();
        lr.Approve();
        _repo.GetByIdAsync(lr.Id, Arg.Any<CancellationToken>()).Returns(lr);
        var handler = new ApproveLeaveRequestHandler(_repo);

        Func<Task> act = () => handler.HandleAsync(lr.Id);

        await act.Should().ThrowAsync<ValidationException>();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // RejectLeaveRequestHandler
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Từ chối đơn Pending phải lưu reviewer note và trả về status Rejected.
    /// </summary>
    [Test]
    public async Task Reject_PendingRequest_RejectsWithReviewerNote()
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
    public async Task Reject_NotFound_ThrowsNotFoundException()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((LeaveRequest?)null);
        var handler = new RejectLeaveRequestHandler(_repo);

        Func<Task> act = () => handler.HandleAsync(Guid.NewGuid(), new RejectLeaveRequestRequest(null));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>
    /// Từ chối đơn đã Rejected rồi phải ném ValidationException (từ entity).
    /// </summary>
    [Test]
    public async Task Reject_AlreadyRejected_ThrowsValidationException()
    {
        var lr = MakeRequest();
        lr.Reject(null);
        _repo.GetByIdAsync(lr.Id, Arg.Any<CancellationToken>()).Returns(lr);
        var handler = new RejectLeaveRequestHandler(_repo);

        Func<Task> act = () => handler.HandleAsync(lr.Id, new RejectLeaveRequestRequest(null));

        await act.Should().ThrowAsync<ValidationException>();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CancelLeaveRequestHandler
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Chính chủ hủy đơn Pending của mình phải thành công và trả về status Cancelled.
    /// </summary>
    [Test]
    public async Task Cancel_OwnerCancelsPendingRequest_ReturnsCancelled()
    {
        var userId = Guid.NewGuid();
        var lr = MakeRequest(userId: userId);
        _repo.GetByIdAsync(lr.Id, Arg.Any<CancellationToken>()).Returns(lr);
        var handler = new CancelLeaveRequestHandler(_repo);

        var result = await handler.HandleAsync(lr.Id, userId);

        result.Status.Should().Be("Cancelled");
    }

    /// <summary>
    /// Hủy đơn không tồn tại phải ném NotFoundException.
    /// </summary>
    [Test]
    public async Task Cancel_NotFound_ThrowsNotFoundException()
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
    public async Task Cancel_DifferentUser_ThrowsValidationException()
    {
        var lr = MakeRequest(userId: Guid.NewGuid());
        _repo.GetByIdAsync(lr.Id, Arg.Any<CancellationToken>()).Returns(lr);
        var handler = new CancelLeaveRequestHandler(_repo);

        Func<Task> act = () => handler.HandleAsync(lr.Id, Guid.NewGuid()); // khác userId

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>
    /// Hủy đơn đã được duyệt (Approved) phải ném ValidationException (từ entity),
    /// không cho hủy đơn đã xử lý xong.
    /// </summary>
    [Test]
    public async Task Cancel_ApprovedRequest_ThrowsValidationException()
    {
        var userId = Guid.NewGuid();
        var lr = MakeRequest(userId: userId);
        lr.Approve();
        _repo.GetByIdAsync(lr.Id, Arg.Any<CancellationToken>()).Returns(lr);
        var handler = new CancelLeaveRequestHandler(_repo);

        Func<Task> act = () => handler.HandleAsync(lr.Id, userId);

        await act.Should().ThrowAsync<ValidationException>();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetLeaveRequestsHandler
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Không có filter trả về toàn bộ danh sách đơn nghỉ phép.
    /// </summary>
    [Test]
    public async Task GetAll_NoFilters_ReturnsAll()
    {
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<LeaveRequest>
        {
            MakeRequest(), MakeRequest(), MakeRequest(),
        });
        var handler = new GetLeaveRequestsHandler(_repo);

        var result = await handler.HandleAsync(null, null);

        result.Should().HaveCount(3);
    }

    /// <summary>
    /// Filter theo status chỉ trả về đơn có đúng trạng thái đó (so sánh không phân biệt hoa thường).
    /// </summary>
    [Test]
    public async Task GetAll_FilterByStatus_ReturnsOnlyMatchingStatus()
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
    public async Task GetAll_NoMatchingFilter_ReturnsEmpty()
    {
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<LeaveRequest> { MakeRequest() });
        var handler = new GetLeaveRequestsHandler(_repo);

        var result = await handler.HandleAsync(status: "Approved", null);

        result.Should().BeEmpty();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetMyLeaveRequestsHandler
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Tổng số ngày nghỉ phép năm luôn là 12, là hằng số quy định của công ty.
    /// </summary>
    [Test]
    public async Task GetMy_TotalAnnualDaysIs12()
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
    public async Task GetMy_UsedAnnualDays_CountsOnlyApprovedAnnualThisYear()
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

        result.Stats.UsedAnnualDays.Should().Be(3); // chỉ approvedAnnual
    }

    /// <summary>
    /// PendingCount phải đếm đúng số đơn đang chờ xử lý của user,
    /// giúp user biết còn bao nhiêu đơn chưa được duyệt.
    /// </summary>
    [Test]
    public async Task GetMy_PendingCount_CountsOnlyPendingRequests()
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

    // ═══════════════════════════════════════════════════════════════════════════
    // GetLeaveRequestByIdHandler
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Lấy đơn theo ID tồn tại phải trả về đúng DTO.
    /// </summary>
    [Test]
    public async Task GetById_ExistingRequest_ReturnsDto()
    {
        var lr = MakeRequest();
        _repo.GetByIdAsync(lr.Id, Arg.Any<CancellationToken>()).Returns(lr);
        var handler = new GetLeaveRequestByIdHandler(_repo);

        var result = await handler.HandleAsync(lr.Id);

        result.Id.Should().Be(lr.Id);
    }

    /// <summary>
    /// ID không tồn tại phải ném NotFoundException.
    /// </summary>
    [Test]
    public async Task GetById_NotFound_ThrowsNotFoundException()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((LeaveRequest?)null);
        var handler = new GetLeaveRequestByIdHandler(_repo);

        Func<Task> act = () => handler.HandleAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════════════════════

    private static LeaveRequest MakeRequest(
        Guid? userId = null,
        LeaveType type = LeaveType.Annual,
        int daysCount = 3)
    {
        var uid = userId ?? Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.Today);
        var lr = LeaveRequest.Create(uid, type, today, today.AddDays(daysCount - 1), "Lý do test");

        // User là navigation property (private set), được EF Core gán trong production.
        // Dùng reflection để set trong test, cần thiết vì ToDto() truy cập r.User.FullName.
        var user = User.Create("emp", "emp@test.com", "hash", "Staff", null, "Nhân Viên Test");
        typeof(LeaveRequest).GetProperty("User")!.SetValue(lr, user);

        return lr;
    }
}
