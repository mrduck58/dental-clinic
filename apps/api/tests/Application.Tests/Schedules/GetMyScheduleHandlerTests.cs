using DentalClinic.API.Application.UseCases.Schedules;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Interfaces.Repositories;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Schedules;

/// <summary>
/// GetMyScheduleHandler dùng chung cho cả Dentist và Staff. Nối lịch với người dùng qua EmployeeId
/// (khóa thật) trước, StaffName (chuẩn hóa qua StaffNameMatcher) chỉ là lưới an toàn cho dòng lịch
/// cũ/nhập tay chưa gán được EmployeeId — cùng chiến lược với GetWaitingQueueHandler.
/// </summary>
[TestFixture]
public class GetMyScheduleHandlerTests
{
    private IUserRepository _userRepo = null!;
    private IWorkScheduleRepository _workScheduleRepo = null!;
    private GetMyScheduleHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _userRepo = Substitute.For<IUserRepository>();
        _workScheduleRepo = Substitute.For<IWorkScheduleRepository>();
        _handler = new GetMyScheduleHandler(_userRepo, _workScheduleRepo);
    }

    private static User MakeUser(string? fullName, UserRole role = UserRole.Dentist, string email = "user@test.com") =>
        User.Create($"u-{Guid.NewGuid()}", email, "hash", role, fullName: fullName);

    private static WorkSchedule MakeSchedule(DateOnly date, string staffName, Guid? employeeId = null)
        => WorkSchedule.Create(date, "Sáng", "Khám", "Nha sĩ", staffName, "Phòng 1", "#FFFFFF", false, employeeId);

    /// <summary>Định dạng ngày không hợp lệ phải ném ArgumentException, không gọi repository.</summary>
    [Test]
    public async Task HandleAsync_InvalidDateFormat_ThrowsArgumentException()
    {
        Func<Task> act = () => _handler.Handle(new GetMyScheduleQuery(Guid.NewGuid(), "16/06/2026"), CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
        await _userRepo.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Tài khoản không tồn tại (user == null) phải trả về danh sách rỗng, không ném lỗi.</summary>
    [Test]
    public async Task HandleAsync_UserNotFound_ReturnsEmptyList()
    {
        _userRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await _handler.Handle(new GetMyScheduleQuery(Guid.NewGuid(), "2026-06-16"), CancellationToken.None);

        result.Should().BeEmpty();
        await _workScheduleRepo.DidNotReceive().GetByDateRangeAsync(
            Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Nối lịch với người dùng qua EmployeeId — khóa thật — kể cả khi StaffName ghi trên dòng lịch
    /// (do người xếp lịch gõ tay) không khớp tuyệt đối với FullName của tài khoản.
    /// </summary>
    [Test]
    public async Task HandleAsync_MatchesByEmployeeId_ReturnsMappedEntriesEvenWhenStaffNameDiffers()
    {
        var userId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var user = MakeUser("Trần Văn Hùng");
        var employee = Employee.Create(userId, "DT-01");
        typeof(Employee).GetProperty(nameof(Employee.Id))!.SetValue(employee, employeeId);
        user.AttachEmployee(employee);
        var weekStart = new DateOnly(2026, 6, 16);
        _userRepo.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);
        _workScheduleRepo.GetByDateRangeAsync(weekStart, weekStart.AddDays(6), Arg.Any<CancellationToken>())
            .Returns(new List<WorkSchedule>
            {
                // StaffName lệch hẳn (chức danh khác kiểu gõ) so với FullName — vẫn phải khớp vì có EmployeeId.
                MakeSchedule(weekStart, "BS. Hùng", employeeId),
                MakeSchedule(weekStart.AddDays(1), "BS. Hùng", employeeId),
                // Dòng của người khác (EmployeeId khác) không được lẫn vào.
                MakeSchedule(weekStart.AddDays(2), "Trần Văn Hùng", Guid.NewGuid()),
            });

        var result = (await _handler.Handle(new GetMyScheduleQuery(userId, "2026-06-16"), CancellationToken.None)).ToList();

        result.Should().HaveCount(2);
    }

    /// <summary>
    /// Dòng lịch chưa gán EmployeeId (dữ liệu cũ/nhập tay) thì rơi xuống so khớp StaffName đã chuẩn
    /// hóa qua StaffNameMatcher — bỏ qua chức danh ("BS.") và khoảng trắng thừa.
    /// </summary>
    [Test]
    public async Task HandleAsync_NoEmployeeIdOnRow_FallsBackToNormalizedStaffNameMatch()
    {
        var userId = Guid.NewGuid();
        var user = MakeUser("Trần Văn Hùng"); // Không gắn Employee — mô phỏng tài khoản chưa có hồ sơ Employee.
        var weekStart = new DateOnly(2026, 6, 16);
        _userRepo.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);
        _workScheduleRepo.GetByDateRangeAsync(weekStart, weekStart.AddDays(6), Arg.Any<CancellationToken>())
            .Returns(new List<WorkSchedule>
            {
                MakeSchedule(weekStart, "BS.  Trần Văn Hùng"), // chức danh + khoảng trắng thừa, EmployeeId null.
                MakeSchedule(weekStart.AddDays(1), "Nguyễn Thị Lan"), // người khác — không khớp.
            });

        var result = (await _handler.Handle(new GetMyScheduleQuery(userId, "2026-06-16"), CancellationToken.None)).ToList();

        result.Should().ContainSingle();
    }

    /// <summary>
    /// Dòng lịch ĐÃ gán EmployeeId (của người khác) thì không được lẫn vào chỉ vì StaffName tình cờ
    /// khớp — EmployeeId khi đã có là căn cứ duy nhất cho dòng đó, không rơi xuống so tên nữa.
    /// </summary>
    [Test]
    public async Task HandleAsync_RowHasEmployeeIdBelongingToSomeoneElse_DoesNotMatchByCoincidentalName()
    {
        var userId = Guid.NewGuid();
        var user = MakeUser("Trần Văn Hùng"); // Không gắn Employee.
        var weekStart = new DateOnly(2026, 6, 16);
        _userRepo.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);
        _workScheduleRepo.GetByDateRangeAsync(weekStart, weekStart.AddDays(6), Arg.Any<CancellationToken>())
            .Returns(new List<WorkSchedule>
            {
                MakeSchedule(weekStart, "Trần Văn Hùng", Guid.NewGuid()),
            });

        var result = await _handler.Handle(new GetMyScheduleQuery(userId, "2026-06-16"), CancellationToken.None);

        result.Should().BeEmpty();
    }

    /// <summary>Nhân viên (Staff) gọi cùng endpoint phải hoạt động y hệt nha sĩ — không phân biệt vai trò.</summary>
    [Test]
    public async Task HandleAsync_StaffUser_ReturnsMappedEntries()
    {
        var userId = Guid.NewGuid();
        var user = MakeUser("Nguyễn Thị Lan", UserRole.Staff);
        var weekStart = new DateOnly(2026, 6, 16);
        _userRepo.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);
        _workScheduleRepo.GetByDateRangeAsync(weekStart, weekStart.AddDays(6), Arg.Any<CancellationToken>())
            .Returns(new List<WorkSchedule> { MakeSchedule(weekStart, "Nguyễn Thị Lan") });

        var result = (await _handler.Handle(new GetMyScheduleQuery(userId, "2026-06-16"), CancellationToken.None)).ToList();

        result.Should().HaveCount(1);
    }

    /// <summary>FullName rỗng/null phải fallback về Email khi so khớp StaffName.</summary>
    [Test]
    public async Task HandleAsync_UserFullNameEmpty_FallsBackToEmailForNameMatching()
    {
        var userId = Guid.NewGuid();
        var user = MakeUser(null, email: "lan@clinic.com");
        var weekStart = new DateOnly(2026, 6, 16);
        _userRepo.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);
        _workScheduleRepo.GetByDateRangeAsync(weekStart, weekStart.AddDays(6), Arg.Any<CancellationToken>())
            .Returns(new List<WorkSchedule> { MakeSchedule(weekStart, "lan@clinic.com") });

        var result = await _handler.Handle(new GetMyScheduleQuery(userId, "2026-06-16"), CancellationToken.None);

        result.Should().ContainSingle();
    }
}
