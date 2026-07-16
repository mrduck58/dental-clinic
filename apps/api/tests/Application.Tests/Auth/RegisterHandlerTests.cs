using DentalClinic.API.Application.UseCases.Auth;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Auth;

[TestFixture]
public class RegisterHandlerTests
{
    private IUserRepository _userRepo = null!;
    private IOtpRepository _otpRepo = null!;
    private IEmailService _emailService = null!;
    private RegisterHandler _handler = null!;

    private const string TestEmail = "benhnhan@test.com";
    private static readonly RegisterCommand ValidCommand = new(TestEmail, "Password123!");

    [SetUp]
    public void SetUp()
    {
        _userRepo = Substitute.For<IUserRepository>();
        _otpRepo = Substitute.For<IOtpRepository>();
        _emailService = Substitute.For<IEmailService>();
        _handler = new RegisterHandler(_userRepo, _otpRepo, _emailService);

        _userRepo.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
    }

    // ── Happy path ────────────────────────────────────────────────────────────

    /// <summary>
    /// Đăng ký email mới phải gọi AddAsync đúng 1 lần để lưu user vào database,
    /// không gọi thừa tạo trùng lặp.
    /// </summary>
    [Test]
    public async Task HandleAsync_NewEmail_CallsAddAsyncOnce()
    {
        await _handler.HandleAsync(ValidCommand);

        await _userRepo.Received(1).AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// User mới phải được tạo với IsActive = false vì chưa qua xác thực OTP,
    /// tránh tài khoản chưa xác thực có thể đăng nhập vào hệ thống.
    /// </summary>
    [Test]
    public async Task HandleAsync_NewEmail_CreatesUserWithIsActiveFalse()
    {
        await _handler.HandleAsync(ValidCommand);

        await _userRepo.Received(1).AddAsync(
            Arg.Is<User>(u => !u.IsActive),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// User mới phải có Role = "Patient", không được gán nhầm role admin hay nhân viên.
    /// </summary>
    [Test]
    public async Task HandleAsync_NewEmail_CreatesUserWithPatientRole()
    {
        await _handler.HandleAsync(ValidCommand);

        await _userRepo.Received(1).AddAsync(
            Arg.Is<User>(u => u.Role == "Patient"),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// OTP cũ của email này phải bị vô hiệu hóa trước khi tạo OTP mới,
    /// tránh nhiều OTP hợp lệ cùng tồn tại một lúc.
    /// </summary>
    [Test]
    public async Task HandleAsync_NewEmail_InvalidatesPreviousOtps()
    {
        await _handler.HandleAsync(ValidCommand);

        await _otpRepo.Received(1).InvalidateAllAsync(
            TestEmail, OtpPurpose.Registration, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Sau khi tạo user và OTP, phải gửi email chứa mã OTP đến đúng địa chỉ
    /// người dùng vừa đăng ký để họ có thể xác thực tài khoản.
    /// </summary>
    [Test]
    public async Task HandleAsync_NewEmail_SendsOtpEmailToRegisteredAddress()
    {
        await _handler.HandleAsync(ValidCommand);

        await _emailService.Received(1).SendOtpAsync(
            TestEmail,
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Response phải trả về đúng email vừa đăng ký để client biết gửi OTP đến đâu.
    /// </summary>
    [Test]
    public async Task HandleAsync_NewEmail_ReturnsResponseWithCorrectEmail()
    {
        var result = await _handler.HandleAsync(ValidCommand);

        result.Email.Should().Be(TestEmail);
    }

    /// <summary>
    /// Sau khi tạo OTP mới, phải lưu OTP vào database đúng 1 lần,
    /// tránh trường hợp email chứa OTP được gửi đi nhưng OTP không được lưu tương ứng.
    /// </summary>
    [Test]
    public async Task HandleAsync_NewEmail_SavesOtpOnce()
    {
        await _handler.HandleAsync(ValidCommand);

        await _otpRepo.Received(1).AddAsync(Arg.Any<OtpCode>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Response trả về phải chứa message hướng dẫn kiểm tra email để lấy mã OTP,
    /// giúp client hiển thị đúng nội dung hướng dẫn cho người dùng vừa đăng ký.
    /// </summary>
    [Test]
    public async Task HandleAsync_NewEmail_ReturnsMessageAboutCheckingEmail()
    {
        var result = await _handler.HandleAsync(ValidCommand);

        result.Message.Should().Contain("email");
    }

    /// <summary>
    /// User Patient mới tạo phải có Username trùng với Email vì bệnh nhân đăng ký
    /// chưa có username riêng — RegisterHandler dùng chính email làm username.
    /// </summary>
    [Test]
    public async Task HandleAsync_NewEmail_CreatesUserWithUsernameEqualToEmail()
    {
        await _handler.HandleAsync(ValidCommand);

        await _userRepo.Received(1).AddAsync(
            Arg.Is<User>(u => u.Username == TestEmail),
            Arg.Any<CancellationToken>());
    }

    // ── Error paths ───────────────────────────────────────────────────────────

    /// <summary>
    /// Email đã tồn tại trong hệ thống phải ném ConflictException,
    /// không cho phép tạo tài khoản trùng lặp.
    /// </summary>
    [Test]
    public async Task HandleAsync_DuplicateEmail_ThrowsConflictException()
    {
        _userRepo.ExistsByEmailAsync(TestEmail, Arg.Any<CancellationToken>()).Returns(true);

        Func<Task> act = () => _handler.HandleAsync(ValidCommand);

        await act.Should().ThrowAsync<ConflictException>();
    }

    /// <summary>
    /// Khi email trùng, handler phải dừng ngay — không được lưu user vào database.
    /// </summary>
    [Test]
    public async Task HandleAsync_DuplicateEmail_DoesNotCallAddAsync()
    {
        _userRepo.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        Assert.CatchAsync(() => _handler.HandleAsync(ValidCommand));

        await _userRepo.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Khi email trùng, không được gửi OTP để tránh làm lộ thông tin
    /// rằng email đó đã có tài khoản trong hệ thống.
    /// </summary>
    [Test]
    public async Task HandleAsync_DuplicateEmail_DoesNotSendOtp()
    {
        _userRepo.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        Assert.CatchAsync(() => _handler.HandleAsync(ValidCommand));

        await _emailService.DidNotReceive().SendOtpAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
