using DentalClinic.API.Application.UseCases.Auth;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Auth;

[TestFixture]
public class ForgotPasswordHandlerTests
{
    private IUserRepository _userRepo = null!;
    private IEmailService _emailService = null!;
    private IConfiguration _configuration = null!;
    private ForgotPasswordHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _userRepo = Substitute.For<IUserRepository>();
        _emailService = Substitute.For<IEmailService>();
        _configuration = Substitute.For<IConfiguration>();
        _configuration["FrontendBaseUrl"].Returns("http://localhost:3000");
        _handler = new ForgotPasswordHandler(_userRepo, _emailService, _configuration);
    }

    // ── Happy path ────────────────────────────────────────────────────────────

    /// <summary>
    /// Email hợp lệ của nhân viên active phải kích hoạt lưu token vào DB
    /// và gửi email đặt lại mật khẩu — đây là luồng chính của forgot password.
    /// </summary>
    [Test]
    public async Task HandleAsync_ValidStaffEmail_SavesTokenAndSendsEmail()
    {
        var user = CreateActiveStaff("staff@test.com", "Admin");
        _userRepo.GetByEmailAsync("staff@test.com", Arg.Any<CancellationToken>()).Returns(user);

        await _handler.HandleAsync(new ForgotPasswordCommand("staff@test.com"));

        await _userRepo.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
        await _emailService.Received(1).SendPasswordResetAsync(
            user.Email,
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Reset link gửi đi phải chứa đúng frontend base URL và tham số email,
    /// để trang reset-password có thể đọc email từ query string.
    /// </summary>
    [Test]
    public async Task HandleAsync_ValidStaffEmail_SendsLinkWithCorrectBaseUrl()
    {
        var user = CreateActiveStaff("staff@clinic.com", "Dentist");
        _userRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);

        await _handler.HandleAsync(new ForgotPasswordCommand("staff@clinic.com"));

        await _emailService.Received(1).SendPasswordResetAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Is<string>(link => link.StartsWith("http://localhost:3000/auth/reset-password")),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Sau khi gọi handler, user entity phải có PasswordResetToken được set,
    /// xác nhận rằng token đã được ghi lên entity trước khi UpdateAsync.
    /// </summary>
    [Test]
    public async Task HandleAsync_ValidStaffEmail_SetsPasswordResetTokenOnUser()
    {
        var user = CreateActiveStaff("staff@test.com", "Staff");
        _userRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);

        await _handler.HandleAsync(new ForgotPasswordCommand("staff@test.com"));

        user.PasswordResetToken.Should().NotBeNullOrEmpty();
        user.PasswordResetTokenExpiry.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Khi cấu hình không có "FrontendBaseUrl", handler phải dùng URL mặc định
    /// "http://localhost:3000" để tạo reset link thay vì throw hoặc để trống.
    /// </summary>
    [Test]
    public async Task HandleAsync_NoFrontendBaseUrlConfigured_UsesDefaultLocalhostInLink()
    {
        _configuration["FrontendBaseUrl"].Returns((string?)null);
        var user = CreateActiveStaff("staff@test.com", "Admin");
        _userRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);

        await _handler.HandleAsync(new ForgotPasswordCommand("staff@test.com"));

        await _emailService.Received(1).SendPasswordResetAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Is<string>(link => link.StartsWith("http://localhost:3000/auth/reset-password")),
            Arg.Any<CancellationToken>());
    }

    // ── Display-name fallback chain (FullName ?? Username ?? Email) ─────────

    /// <summary>
    /// Khi user có FullName, email phải được gửi kèm FullName làm tên hiển thị.
    /// </summary>
    [Test]
    public async Task HandleAsync_UserHasFullName_SendsEmailWithFullNameAsDisplayName()
    {
        var hash = BCrypt.Net.BCrypt.HashPassword("anypass");
        var user = User.Create("user1", "staff@test.com", hash, "Admin", fullName: "Nguyễn Văn A");
        _userRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);

        await _handler.HandleAsync(new ForgotPasswordCommand("staff@test.com"));

        await _emailService.Received(1).SendPasswordResetAsync(
            Arg.Any<string>(), "Nguyễn Văn A", Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Khi user không có FullName nhưng có Username, email phải được gửi kèm Username
    /// làm tên hiển thị (fallback thứ hai trong chuỗi FullName ?? Username ?? Email).
    /// </summary>
    [Test]
    public async Task HandleAsync_UserHasNoFullNameButHasUsername_SendsEmailWithUsernameAsDisplayName()
    {
        var user = CreateActiveStaff("staff@test.com", "Staff"); // FullName null, Username = "user1"
        _userRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);

        await _handler.HandleAsync(new ForgotPasswordCommand("staff@test.com"));

        await _emailService.Received(1).SendPasswordResetAsync(
            Arg.Any<string>(), "user1", Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Khi user không có cả FullName lẫn Username, email phải được gửi kèm Email
    /// làm tên hiển thị (fallback cuối cùng trong chuỗi FullName ?? Username ?? Email).
    /// </summary>
    [Test]
    public async Task HandleAsync_UserHasNoFullNameAndNoUsername_SendsEmailWithEmailAsDisplayName()
    {
        var user = User.CreateEmployee("staff@test.com", "Staff"); // Username null, FullName null
        _userRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);

        await _handler.HandleAsync(new ForgotPasswordCommand("staff@test.com"));

        await _emailService.Received(1).SendPasswordResetAsync(
            Arg.Any<string>(), "staff@test.com", Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ── Silent-fail cases (không tiết lộ email tồn tại hay không) ────────────

    /// <summary>
    /// Email không tồn tại trong hệ thống phải trả về thành công im lặng,
    /// không ném exception và không gửi email — tránh kẻ tấn công dò email hợp lệ.
    /// </summary>
    [Test]
    public async Task HandleAsync_EmailNotFound_CompletesWithoutSendingEmail()
    {
        _userRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        Func<Task> act = () => _handler.HandleAsync(new ForgotPasswordCommand("notfound@test.com"));

        await act.Should().NotThrowAsync();
        await _emailService.DidNotReceive().SendPasswordResetAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Tài khoản bệnh nhân (role Patient) không được đặt lại mật khẩu qua trang admin,
    /// handler phải im lặng bỏ qua — tính năng này chỉ dành cho nhân viên.
    /// </summary>
    [Test]
    public async Task HandleAsync_PatientRole_CompletesWithoutSendingEmail()
    {
        var patient = CreateActiveStaff("patient@test.com", "Patient");
        _userRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(patient);

        Func<Task> act = () => _handler.HandleAsync(new ForgotPasswordCommand("patient@test.com"));

        await act.Should().NotThrowAsync();
        await _emailService.DidNotReceive().SendPasswordResetAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Tài khoản bị vô hiệu hóa (IsActive = false) phải bị bỏ qua im lặng,
    /// không để kẻ tấn công biết rằng email này tồn tại nhưng bị khóa.
    /// </summary>
    [Test]
    public async Task HandleAsync_InactiveUser_CompletesWithoutSendingEmail()
    {
        var user = CreateActiveStaff("staff@test.com", "Admin");
        user.SetActive(false);
        _userRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);

        Func<Task> act = () => _handler.HandleAsync(new ForgotPasswordCommand("staff@test.com"));

        await act.Should().NotThrowAsync();
        await _emailService.DidNotReceive().SendPasswordResetAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Email được trim và lowercase trước khi tra cứu, đảm bảo
    /// "  Staff@Test.COM  " tìm đúng user giống như "staff@test.com".
    /// </summary>
    [Test]
    public async Task HandleAsync_EmailWithWhitespaceAndUpperCase_NormalizesBeforeLookup()
    {
        var user = CreateActiveStaff("staff@test.com", "Staff");
        _userRepo.GetByEmailAsync("staff@test.com", Arg.Any<CancellationToken>()).Returns(user);

        await _handler.HandleAsync(new ForgotPasswordCommand("  Staff@Test.COM  "));

        await _userRepo.Received(1).GetByEmailAsync("staff@test.com", Arg.Any<CancellationToken>());
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static User CreateActiveStaff(string email, string role)
    {
        var hash = BCrypt.Net.BCrypt.HashPassword("anypass");
        return User.Create("user1", email, hash, role);
    }
}
