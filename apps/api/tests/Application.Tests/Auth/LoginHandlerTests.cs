using DentalClinic.API.Application.UseCases.Auth;
using DentalClinic.API.Domain.Entities;
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
    private LoginHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _userRepo = Substitute.For<IUserRepository>();
        _jwtService = Substitute.For<IJwtService>();
        _handler = new LoginHandler(_userRepo, _jwtService);
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

        var result = await _handler.HandleAsync(new LoginCommand(user.Email, "pass123"));

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

        var result = await _handler.HandleAsync(new LoginCommand(user.Email, "pass123"));

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

        var result = await _handler.HandleAsync(new LoginCommand(user.Email, "pass"));

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

        await _handler.HandleAsync(new LoginCommand(user.Email, "pass123"));

        _jwtService.Received(1).GenerateToken(user);
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

        Func<Task> act = () => _handler.HandleAsync(new LoginCommand("notfound@test.com", "pass"));

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

        Func<Task> act = () => _handler.HandleAsync(new LoginCommand(user.Email, "pass123"));

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

        Assert.CatchAsync(() => _handler.HandleAsync(new LoginCommand(user.Email, "pass123")));

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

        Func<Task> act = () => _handler.HandleAsync(new LoginCommand(user.Email, "wrongpass"));

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    /// <summary>
    /// Nhân viên được tạo hồ sơ nhưng chưa được cấp tài khoản (PasswordHash = null)
    /// phải bị từ chối đăng nhập, không được bypass bước xác thực mật khẩu.
    /// </summary>
    [Test]
    public async Task HandleAsync_EmployeeWithNoPasswordHash_ThrowsUnauthorizedAccessException()
    {
        var user = User.CreateEmployee("emp@test.com", "Staff");
        _userRepo.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);

        Func<Task> act = () => _handler.HandleAsync(new LoginCommand(user.Email, "anypass"));

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private static User CreateActiveUserWithPassword(string plainPassword)
    {
        var hash = BCrypt.Net.BCrypt.HashPassword(plainPassword);
        return User.Create("user1", "test@test.com", hash, "Staff");
    }
}
