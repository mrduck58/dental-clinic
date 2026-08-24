using DentalClinic.API.Application.UseCases.Schedules;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Interfaces.Repositories;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Schedules;

/// <summary>
/// GetMyScheduleHandler dùng chung cho cả Dentist và Staff — WorkSchedule chỉ gắn nhân sự bằng tên
/// hiển thị (StaffName), không phân biệt vai trò, nên handler chỉ cần tên của user đang gọi.
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

    private static WorkSchedule MakeSchedule(DateOnly date, string staffName)
        => WorkSchedule.Create(date, "Sáng", "Khám", "Nha sĩ", staffName, "Phòng 1", "#FFFFFF", false);

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
        await _workScheduleRepo.DidNotReceive().GetByStaffNameAndDateRangeAsync(
            Arg.Any<string>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Phải lấy lịch theo đúng tên nha sĩ và đúng khoảng 7 ngày kể từ WeekStart, trả về đủ entry.</summary>
    [Test]
    public async Task HandleAsync_ValidDentistAndWeek_ReturnsMappedEntries()
    {
        var userId = Guid.NewGuid();
        var user = MakeUser("Bs. Trần");
        var weekStart = new DateOnly(2026, 6, 16);
        _userRepo.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);
        _workScheduleRepo.GetByStaffNameAndDateRangeAsync("Bs. Trần", weekStart, weekStart.AddDays(7), Arg.Any<CancellationToken>())
            .Returns(new List<WorkSchedule>
            {
                MakeSchedule(weekStart, "Bs. Trần"),
                MakeSchedule(weekStart.AddDays(1), "Bs. Trần"),
            });

        var result = (await _handler.Handle(new GetMyScheduleQuery(userId, "2026-06-16"), CancellationToken.None)).ToList();

        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Bs. Trần");
        await _workScheduleRepo.Received(1).GetByStaffNameAndDateRangeAsync(
            "Bs. Trần", weekStart, weekStart.AddDays(7), Arg.Any<CancellationToken>());
    }

    /// <summary>Nhân viên (Staff) gọi cùng endpoint phải hoạt động y hệt nha sĩ — WorkSchedule không
    /// phân biệt vai trò, chỉ khớp theo tên.</summary>
    [Test]
    public async Task HandleAsync_StaffUser_ReturnsMappedEntries()
    {
        var userId = Guid.NewGuid();
        var user = MakeUser("Nguyễn Thị Lan", UserRole.Staff);
        var weekStart = new DateOnly(2026, 6, 16);
        _userRepo.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);
        _workScheduleRepo.GetByStaffNameAndDateRangeAsync("Nguyễn Thị Lan", weekStart, weekStart.AddDays(7), Arg.Any<CancellationToken>())
            .Returns(new List<WorkSchedule> { MakeSchedule(weekStart, "Nguyễn Thị Lan") });

        var result = (await _handler.Handle(new GetMyScheduleQuery(userId, "2026-06-16"), CancellationToken.None)).ToList();

        result.Should().HaveCount(1);
    }

    /// <summary>FullName rỗng/null phải fallback về Email khi tra lịch theo tên.</summary>
    [Test]
    public async Task HandleAsync_UserFullNameEmpty_FallsBackToEmail()
    {
        var userId = Guid.NewGuid();
        var user = MakeUser(null, email: "lan@clinic.com");
        var weekStart = new DateOnly(2026, 6, 16);
        _userRepo.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);
        _workScheduleRepo.GetByStaffNameAndDateRangeAsync("lan@clinic.com", weekStart, weekStart.AddDays(7), Arg.Any<CancellationToken>())
            .Returns(new List<WorkSchedule>());

        await _handler.Handle(new GetMyScheduleQuery(userId, "2026-06-16"), CancellationToken.None);

        await _workScheduleRepo.Received(1).GetByStaffNameAndDateRangeAsync(
            "lan@clinic.com", weekStart, weekStart.AddDays(7), Arg.Any<CancellationToken>());
    }
}
