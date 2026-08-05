using DentalClinic.API.Application.UseCases.Auth;
using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Auth;

[TestFixture]
public class LoginHandlerTests
{
    private IUserRepository _userRepo = null!;
    private IJwtService _jwtService = null!;
    private IActivityLogService _activityLog = null!;
    private LoginHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _userRepo = Substitute.For<IUserRepository>();
        _jwtService = Substitute.For<IJwtService>();
        _activityLog = Substitute.For<IActivityLogService>();
        _handler = new LoginHandler(_userRepo, _jwtService, _activityLog);
    }

    // ── Happy path ────────────────────────────────────────────────────────────

    /// <summary>
    /// Đăng nhập thành công với email và mật khẩu hợp lệ phải trả về AccessToken
    /// do JwtService sinh ra, không được trả về chuỗi rỗng hay null.
    /// </summary>
    [Test]
    public async Task HandleAsync_ValidCredentials_ReturnsAccessToken()
    {
        var user = CreateActiveUserWithPassword("pass123");
        _userRepo.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);
        _jwtService.GenerateToken(user).Returns("jwt-token");

        var result = await _handler.Handle(new LoginCommand(user.Email, "pass123"), CancellationToken.None);

        result.AccessToken.Should().Be("jwt-token");
    }

    /// <summary>
    /// Đăng nhập thành công phải trả về thông tin user trong DTO,
    /// bao gồm email, role và trạng thái active — để client hiển thị đúng UI.
    /// </summary>
    [Test]
    public async Task HandleAsync_ValidCredentials_ReturnsUserDto()
    {
        var user = CreateActiveUserWithPassword("pass123");
        _userRepo.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);
        _jwtService.GenerateToken(user).Returns("token");

        var result = await _handler.Handle(new LoginCommand(user.Email, "pass123"), CancellationToken.None);

        result.User.Email.Should().Be(user.Email);
        result.User.Role.Should().Be("Staff");
        result.User.IsActive.Should().BeTrue();
    }

    /// <summary>
    /// ExpiresIn trong response phải là 900 giây (15 phút) đúng theo cấu hình,
    /// để client biết thời điểm token hết hạn và tự refresh.
    /// </summary>
    [Test]
    public async Task HandleAsync_ValidCredentials_ExpiresInIs900Seconds()
    {
        var user = CreateActiveUserWithPassword("pass");
        _userRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);
        _jwtService.GenerateToken(Arg.Any<User>()).Returns("token");

        var result = await _handler.Handle(new LoginCommand(user.Email, "pass"), CancellationToken.None);

        result.ExpiresIn.Should().Be(900); // 15 * 60
    }

    /// <summary>
    /// Handler phải gọi GenerateToken đúng 1 lần với đúng user object,
    /// đảm bảo token được sinh ra cho đúng người dùng vừa xác thực.
    /// </summary>
    [Test]
    public async Task HandleAsync_ValidCredentials_CallsGenerateTokenOnce()
    {
        var user = CreateActiveUserWithPassword("pass123");
        _userRepo.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);
        _jwtService.GenerateToken(Arg.Any<User>()).Returns("token");

        await _handler.Handle(new LoginCommand(user.Email, "pass123"), CancellationToken.None);

        _jwtService.Received(1).GenerateToken(user);
    }

    /// <summary>
    /// Đăng nhập thành công phải ghi log hoạt động đúng 1 lần với action Login
    /// và status Success, để hệ thống lưu vết lịch sử đăng nhập của người dùng.
    /// </summary>
    [Test]
    public async Task HandleAsync_ValidCredentials_LogsSuccessActivityOnce()
    {
        var user = CreateActiveUserWithPassword("pass123");
        _userRepo.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);
        _jwtService.GenerateToken(Arg.Any<User>()).Returns("token");

        await _handler.Handle(new LoginCommand(user.Email, "pass123"), CancellationToken.None);

        await _activityLog.Received(1).LogAsync(
            user.Id,
            Arg.Any<string>(),
            user.Role.ToString(),
            ActivityAction.Login,
            ActivityModule.Account,
            Arg.Any<string>(),
            ActivityStatus.Success,
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Mật khẩu sai phải ghi log hoạt động với status Failed và userId = null,
    /// vì hệ thống chưa xác thực được danh tính người thực hiện đăng nhập.
    /// </summary>
    [Test]
    public async Task HandleAsync_WrongPassword_LogsFailedActivityWithNullUserId()
    {
        var user = CreateActiveUserWithPassword("correctpass");
        _userRepo.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);

        Assert.CatchAsync(() => _handler.Handle(new LoginCommand(user.Email, "wrongpass"), CancellationToken.None));

        await _activityLog.Received(1).LogAsync(
            null,
            Arg.Any<string>(),
            Arg.Any<string>(),
            ActivityAction.Login,
            ActivityModule.Account,
            Arg.Any<string>(),
            ActivityStatus.Failed,
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Tài khoản bị vô hiệu hóa phải ghi log hoạt động với status Failed,
    /// giúp admin theo dõi các lần đăng nhập bị từ chối do tài khoản khóa.
    /// </summary>
    [Test]
    public async Task HandleAsync_InactiveUser_LogsFailedActivity()
    {
        var user = CreateActiveUserWithPassword("pass123");
        user.SetActive(false);
        _userRepo.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);

        Assert.CatchAsync(() => _handler.Handle(new LoginCommand(user.Email, "pass123"), CancellationToken.None));

        await _activityLog.Received(1).LogAsync(
            user.Id,
            Arg.Any<string>(),
            user.Role.ToString(),
            ActivityAction.Login,
            ActivityModule.Account,
            Arg.Any<string>(),
            ActivityStatus.Failed,
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    // ── AllowedRoles: trường hợp hợp lệ ───────────────────────────────────────

    /// <summary>
    /// Khi role của user nằm trong danh sách AllowedRoles, đăng nhập phải thành công
    /// và trả về access token — cổng đăng nhập không được chặn nhầm role hợp lệ.
    /// </summary>
    [Test]
    public async Task HandleAsync_RoleWithinAllowedRoles_ReturnsAccessToken()
    {
        var staff = CreateActiveUserWithPassword("pass123");
        _userRepo.GetByEmailAsync(staff.Email, Arg.Any<CancellationToken>()).Returns(staff);
        _jwtService.GenerateToken(Arg.Any<User>()).Returns("jwt-token");

        var result = await _handler.Handle(
            new LoginCommand(staff.Email, "pass123", AllowedRoles: ["Admin", "Dentist", "Staff"]), CancellationToken.None);

        result.AccessToken.Should().Be("jwt-token");
    }

    /// <summary>
    /// AllowedRoles là mảng rỗng phải được xem như không có giới hạn cổng đăng nhập
    /// (tương đương null), không được vô tình chặn tất cả mọi role.
    /// </summary>
    [Test]
    public async Task HandleAsync_EmptyAllowedRolesArray_DoesNotRestrictLogin()
    {
        var user = CreateActiveUserWithPassword("pass123");
        _userRepo.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);
        _jwtService.GenerateToken(Arg.Any<User>()).Returns("jwt-token");

        var result = await _handler.Handle(
            new LoginCommand(user.Email, "pass123", AllowedRoles: []), CancellationToken.None);

        result.AccessToken.Should().Be("jwt-token");
    }

    // ── Error paths ───────────────────────────────────────────────────────────

    /// <summary>
    /// Nếu không tìm thấy user theo email phải ném UnauthorizedAccessException,
    /// không tiết lộ lý do cụ thể để tránh kẻ tấn công dò email hợp lệ.
    /// </summary>
    [Test]
    public async Task HandleAsync_EmailNotFound_ThrowsUnauthorizedAccessException()
    {
        _userRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        Func<Task> act = () => _handler.Handle(new LoginCommand("notfound@test.com", "pass"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    /// <summary>
    /// Tài khoản bị vô hiệu hóa (IsActive = false) phải bị từ chối đăng nhập
    /// và thông báo rõ lý do để user biết liên hệ quản trị viên.
    /// </summary>
    [Test]
    public async Task HandleAsync_InactiveUser_ThrowsUnauthorizedAccessException()
    {
        var user = CreateActiveUserWithPassword("pass123");
        user.SetActive(false);
        _userRepo.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);

        Func<Task> act = () => _handler.Handle(new LoginCommand(user.Email, "pass123"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*vô hiệu hóa*");
    }

    /// <summary>
    /// Khi tài khoản bị vô hiệu hóa, handler phải dừng ngay và không sinh JWT,
    /// tránh trường hợp token được tạo ra cho tài khoản không được phép truy cập.
    /// </summary>
    [Test]
    public async Task HandleAsync_InactiveUser_DoesNotCallGenerateToken()
    {
        var user = CreateActiveUserWithPassword("pass123");
        user.SetActive(false);
        _userRepo.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);

        Assert.CatchAsync(() => _handler.Handle(new LoginCommand(user.Email, "pass123"), CancellationToken.None));

        _jwtService.DidNotReceive().GenerateToken(Arg.Any<User>());
    }

    /// <summary>
    /// Mật khẩu sai phải ném UnauthorizedAccessException với cùng message
    /// như email không tồn tại, để tránh tiết lộ email nào đã đăng ký.
    /// </summary>
    [Test]
    public async Task HandleAsync_WrongPassword_ThrowsUnauthorizedAccessException()
    {
        var user = CreateActiveUserWithPassword("correctpass");
        _userRepo.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);

        Func<Task> act = () => _handler.Handle(new LoginCommand(user.Email, "wrongpass"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    /// <summary>
    /// Nhân viên được tạo hồ sơ nhưng chưa được cấp tài khoản (PasswordHash = null)
    /// phải bị từ chối đăng nhập, không được bypass bước xác thực mật khẩu.
    /// </summary>
    [Test]
    public async Task HandleAsync_EmployeeWithNoPasswordHash_ThrowsUnauthorizedAccessException()
    {
        var user = User.CreateEmployee("emp@test.com", UserRole.Staff);
        _userRepo.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);

        Func<Task> act = () => _handler.Handle(new LoginCommand(user.Email, "anypass"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    // ── AllowedRoles (tách cổng đăng nhập bệnh nhân / nhân viên) ─────────────

    /// <summary>
    /// Bệnh nhân đăng nhập vào cổng nhân viên (AllowedRoles = ["Admin","Dentist","Staff"])
    /// phải bị từ chối — mỗi cổng chỉ phục vụ đúng nhóm role của mình.
    /// </summary>
    [Test]
    public async Task HandleAsync_PatientWithStaffPortalRoles_ThrowsUnauthorizedAccessException()
    {
        var patient = CreateActivePatientWithPassword("pass123");
        _userRepo.GetByEmailAsync(patient.Email, Arg.Any<CancellationToken>()).Returns(patient);
        _jwtService.GenerateToken(Arg.Any<User>()).Returns("token");

        Func<Task> act = () => _handler.Handle(
            new LoginCommand(patient.Email, "pass123", AllowedRoles: ["Admin", "Dentist", "Staff"]), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    /// <summary>
    /// Nhân viên đăng nhập vào cổng bệnh nhân (AllowedRoles = ["Patient"])
    /// phải bị từ chối — tách biệt luồng xác thực giữa hai nhóm.
    /// </summary>
    [Test]
    public async Task HandleAsync_StaffWithPatientPortalRoles_ThrowsUnauthorizedAccessException()
    {
        var staff = CreateActiveUserWithPassword("pass123");
        _userRepo.GetByEmailAsync(staff.Email, Arg.Any<CancellationToken>()).Returns(staff);
        _jwtService.GenerateToken(Arg.Any<User>()).Returns("token");

        Func<Task> act = () => _handler.Handle(
            new LoginCommand(staff.Email, "pass123", AllowedRoles: ["Patient"]), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    /// <summary>
    /// Tài khoản Patient chưa xác thực OTP (IsActive = false) phải nhận message
    /// hướng dẫn xác thực qua OTP, khác với Staff bị vô hiệu hóa (hướng dẫn liên hệ admin).
    /// </summary>
    [Test]
    public async Task HandleAsync_InactivePatient_ThrowsWithOtpVerificationMessage()
    {
        var patient = CreateActivePatientWithPassword("pass123");
        patient.SetActive(false);
        _userRepo.GetByEmailAsync(patient.Email, Arg.Any<CancellationToken>()).Returns(patient);

        Func<Task> act = () => _handler.Handle(new LoginCommand(patient.Email, "pass123"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*xác thực*");
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private static User CreateActiveUserWithPassword(string plainPassword)
    {
        var hash = BCrypt.Net.BCrypt.HashPassword(plainPassword);
        return User.Create("user1", "test@test.com", hash, UserRole.Staff);
    }

    private static User CreateActivePatientWithPassword(string plainPassword)
    {
        var hash = BCrypt.Net.BCrypt.HashPassword(plainPassword);
        return User.Create("patient1", "patient@test.com", hash, UserRole.Patient);
    }
}
