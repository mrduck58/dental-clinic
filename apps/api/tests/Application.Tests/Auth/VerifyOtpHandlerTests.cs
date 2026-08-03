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
public class VerifyOtpHandlerTests
{
    private IUserRepository _userRepo = null!;
    private IOtpRepository _otpRepo = null!;
    private IJwtService _jwtService = null!;
    private VerifyOtpHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _userRepo = Substitute.For<IUserRepository>();
        _otpRepo = Substitute.For<IOtpRepository>();
        _jwtService = Substitute.For<IJwtService>();
        _jwtService.GenerateToken(Arg.Any<User>()).Returns("fake-jwt-token");
        _handler = new VerifyOtpHandler(_userRepo, _otpRepo, _jwtService);
    }

    /// <summary>Không có OTP hợp lệ (hết hạn/không tồn tại) phải ném UnauthorizedAccessException.</summary>
    [Test]
    public async Task HandleAsync_NoValidOtp_ThrowsUnauthorizedAccessException()
    {
        _otpRepo.GetLatestValidAsync(Arg.Any<string>(), OtpPurpose.Registration, Arg.Any<CancellationToken>())
            .Returns((OtpCode?)null);

        Func<Task> act = () => _handler.Handle(new VerifyOtpCommand("test@test.com", "123456"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    /// <summary>Mã OTP nhập sai (khác mã đã lưu) phải ném UnauthorizedAccessException.</summary>
    [Test]
    public async Task HandleAsync_WrongCode_ThrowsUnauthorizedAccessException()
    {
        var otp = OtpCode.Create("test@test.com", OtpPurpose.Registration);
        _otpRepo.GetLatestValidAsync("test@test.com", OtpPurpose.Registration, Arg.Any<CancellationToken>()).Returns(otp);

        Func<Task> act = () => _handler.Handle(new VerifyOtpCommand("test@test.com", "wrong-code"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        await _otpRepo.DidNotReceive().UpdateAsync(Arg.Any<OtpCode>(), Arg.Any<CancellationToken>());
    }

    /// <summary>OTP đúng nhưng không tìm thấy user tương ứng phải ném NotFoundException.</summary>
    [Test]
    public async Task HandleAsync_UserNotFound_ThrowsNotFoundException()
    {
        var otp = OtpCode.Create("test@test.com", OtpPurpose.Registration);
        _otpRepo.GetLatestValidAsync("test@test.com", OtpPurpose.Registration, Arg.Any<CancellationToken>()).Returns(otp);
        _userRepo.GetByEmailAsync("test@test.com", Arg.Any<CancellationToken>()).Returns((User?)null);

        Func<Task> act = () => _handler.Handle(new VerifyOtpCommand("test@test.com", otp.Code), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>Xác thực OTP hợp lệ phải đánh dấu OTP đã dùng, kích hoạt tài khoản và trả về token.</summary>
    [Test]
    public async Task HandleAsync_ValidOtp_ActivatesUserAndReturnsToken()
    {
        var otp = OtpCode.Create("test@test.com", OtpPurpose.Registration);
        var user = User.Create("patient1", "test@test.com", BCrypt.Net.BCrypt.HashPassword("pass"), "Patient");
        _otpRepo.GetLatestValidAsync("test@test.com", OtpPurpose.Registration, Arg.Any<CancellationToken>()).Returns(otp);
        _userRepo.GetByEmailAsync("test@test.com", Arg.Any<CancellationToken>()).Returns(user);

        var result = await _handler.Handle(new VerifyOtpCommand("test@test.com", otp.Code), CancellationToken.None);

        user.IsActive.Should().BeTrue();
        result.AccessToken.Should().Be("fake-jwt-token");
        await _otpRepo.Received(1).UpdateAsync(Arg.Is<OtpCode>(o => o.IsUsed), Arg.Any<CancellationToken>());
        await _userRepo.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
    }

    /// <summary>Tài khoản đang ở trạng thái chưa active thì sau khi xác thực OTP phải được kích hoạt thành active.</summary>
    [Test]
    public async Task HandleAsync_ValidOtp_ActivatesInactiveUser()
    {
        var otp = OtpCode.Create("test@test.com", OtpPurpose.Registration);
        var user = User.Create("patient1", "test@test.com", BCrypt.Net.BCrypt.HashPassword("pass"), "Patient");
        user.SetActive(false);
        _otpRepo.GetLatestValidAsync("test@test.com", OtpPurpose.Registration, Arg.Any<CancellationToken>()).Returns(otp);
        _userRepo.GetByEmailAsync("test@test.com", Arg.Any<CancellationToken>()).Returns(user);

        await _handler.Handle(new VerifyOtpCommand("test@test.com", otp.Code), CancellationToken.None);

        user.IsActive.Should().BeTrue();
        await _userRepo.Received(1).UpdateAsync(Arg.Is<User>(u => u.IsActive), Arg.Any<CancellationToken>());
    }

    /// <summary>Kết quả trả về phải chứa đầy đủ thông tin: Id, Email, Role của user và thời hạn token 8 tiếng.</summary>
    [Test]
    public async Task HandleAsync_ValidOtp_ReturnsFullResponseDto()
    {
        var otp = OtpCode.Create("test@test.com", OtpPurpose.Registration);
        var user = User.Create("patient1", "test@test.com", BCrypt.Net.BCrypt.HashPassword("pass"), "Patient");
        _otpRepo.GetLatestValidAsync("test@test.com", OtpPurpose.Registration, Arg.Any<CancellationToken>()).Returns(otp);
        _userRepo.GetByEmailAsync("test@test.com", Arg.Any<CancellationToken>()).Returns(user);

        var result = await _handler.Handle(new VerifyOtpCommand("test@test.com", otp.Code), CancellationToken.None);

        result.Id.Should().Be(user.Id);
        result.Email.Should().Be(user.Email);
        result.Role.Should().Be(user.Role);
        result.ExpiresIn.Should().Be(8 * 60 * 60);
    }

    /// <summary>Dù user không tồn tại, OTP vẫn phải được đánh dấu đã dùng trước khi ném NotFoundException.</summary>
    [Test]
    public async Task HandleAsync_UserNotFound_StillMarksOtpAsUsed()
    {
        var otp = OtpCode.Create("test@test.com", OtpPurpose.Registration);
        _otpRepo.GetLatestValidAsync("test@test.com", OtpPurpose.Registration, Arg.Any<CancellationToken>()).Returns(otp);
        _userRepo.GetByEmailAsync("test@test.com", Arg.Any<CancellationToken>()).Returns((User?)null);

        Func<Task> act = () => _handler.Handle(new VerifyOtpCommand("test@test.com", otp.Code), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await _otpRepo.Received(1).UpdateAsync(Arg.Is<OtpCode>(o => o.IsUsed), Arg.Any<CancellationToken>());
        await _userRepo.DidNotReceive().UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }
}
