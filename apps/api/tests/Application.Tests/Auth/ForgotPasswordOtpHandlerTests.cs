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
public class ForgotPasswordOtpHandlerTests
{
    private IUserRepository _userRepo = null!;
    private IOtpRepository _otpRepo = null!;
    private IEmailService _emailService = null!;
    private ForgotPasswordOtpHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _userRepo = Substitute.For<IUserRepository>();
        _otpRepo = Substitute.For<IOtpRepository>();
        _emailService = Substitute.For<IEmailService>();
        _handler = new ForgotPasswordOtpHandler(_userRepo, _otpRepo, _emailService);
    }

    private static User CreatePatient(string email, bool isActive = true)
    {
        var user = User.Create("patient1", email, BCrypt.Net.BCrypt.HashPassword("pass"), UserRole.Patient);
        user.SetActive(isActive);
        return user;
    }

    /// <summary>Email chưa đăng ký phải ném NotFoundException, không gửi OTP.</summary>
    [Test]
    public async Task HandleAsync_EmailNotFound_ThrowsNotFoundException()
    {
        _userRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        Func<Task> act = () => _handler.Handle(new ForgotPasswordOtpCommand("notfound@test.com"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await _emailService.DidNotReceive().SendPasswordResetOtpAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Tài khoản không phải role Patient (nhân viên) phải bị chặn — luồng OTP chỉ cho mobile app.</summary>
    [Test]
    public async Task HandleAsync_NonPatientRole_ThrowsNotFoundException()
    {
        var staff = User.Create("staff1", "staff@test.com", BCrypt.Net.BCrypt.HashPassword("pass"), UserRole.Staff);
        _userRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(staff);

        Func<Task> act = () => _handler.Handle(new ForgotPasswordOtpCommand("staff@test.com"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>Tài khoản đã bị vô hiệu hóa phải bị chặn, không gửi OTP.</summary>
    [Test]
    public async Task HandleAsync_InactiveAccount_ThrowsNotFoundException()
    {
        var patient = CreatePatient("inactive@test.com", isActive: false);
        _userRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(patient);

        Func<Task> act = () => _handler.Handle(new ForgotPasswordOtpCommand("inactive@test.com"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>Yêu cầu hợp lệ phải vô hiệu hóa OTP cũ, tạo OTP mới và gửi email đúng địa chỉ.</summary>
    [Test]
    public async Task HandleAsync_ValidPatientEmail_InvalidatesOldOtpAndSendsNewOne()
    {
        var patient = CreatePatient("patient@test.com");
        _userRepo.GetByEmailAsync("patient@test.com", Arg.Any<CancellationToken>()).Returns(patient);

        await _handler.Handle(new ForgotPasswordOtpCommand("patient@test.com"), CancellationToken.None);

        await _otpRepo.Received(1).InvalidateAllAsync("patient@test.com", OtpPurpose.PasswordReset, Arg.Any<CancellationToken>());
        await _otpRepo.Received(1).AddAsync(Arg.Any<OtpCode>(), Arg.Any<CancellationToken>());
        await _emailService.Received(1).SendPasswordResetOtpAsync("patient@test.com", Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Email nhập hoa/thường và có khoảng trắng phải được chuẩn hóa trước khi tra cứu.</summary>
    [Test]
    public async Task HandleAsync_EmailWithWhitespaceAndUpperCase_NormalizesBeforeLookup()
    {
        var patient = CreatePatient("patient@test.com");
        _userRepo.GetByEmailAsync("patient@test.com", Arg.Any<CancellationToken>()).Returns(patient);

        await _handler.Handle(new ForgotPasswordOtpCommand("  Patient@Test.COM  "), CancellationToken.None);

        await _userRepo.Received(1).GetByEmailAsync("patient@test.com", Arg.Any<CancellationToken>());
    }

    /// <summary>Mã OTP gửi qua email phải trùng khớp với mã OTP đã được lưu vào repository, không phải mã ngẫu nhiên khác.</summary>
    [Test]
    public async Task HandleAsync_ValidPatientEmail_SentOtpCodeMatchesStoredCode()
    {
        var patient = CreatePatient("patient@test.com");
        _userRepo.GetByEmailAsync("patient@test.com", Arg.Any<CancellationToken>()).Returns(patient);
        OtpCode? capturedOtp = null;
        await _otpRepo.AddAsync(Arg.Do<OtpCode>(o => capturedOtp = o), Arg.Any<CancellationToken>());

        await _handler.Handle(new ForgotPasswordOtpCommand("patient@test.com"), CancellationToken.None);

        capturedOtp.Should().NotBeNull();
        capturedOtp!.Email.Should().Be("patient@test.com");
        capturedOtp.Purpose.Should().Be(OtpPurpose.PasswordReset);
        await _emailService.Received(1).SendPasswordResetOtpAsync("patient@test.com", capturedOtp.Code, Arg.Any<CancellationToken>());
    }

    /// <summary>Khi email không tồn tại, không được tạo hay lưu mã OTP mới.</summary>
    [Test]
    public async Task HandleAsync_EmailNotFound_DoesNotCreateOtp()
    {
        _userRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        Func<Task> act = () => _handler.Handle(new ForgotPasswordOtpCommand("notfound@test.com"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await _otpRepo.DidNotReceive().InvalidateAllAsync(Arg.Any<string>(), Arg.Any<OtpPurpose>(), Arg.Any<CancellationToken>());
        await _otpRepo.DidNotReceive().AddAsync(Arg.Any<OtpCode>(), Arg.Any<CancellationToken>());
    }
}
