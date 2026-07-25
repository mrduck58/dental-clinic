using DentalClinic.API.Application.UseCases.Auth;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Interfaces.Repositories;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Auth;

[TestFixture]
public class VerifyPasswordResetOtpHandlerTests
{
    private IUserRepository _userRepo = null!;
    private IOtpRepository _otpRepo = null!;
    private VerifyPasswordResetOtpHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _userRepo = Substitute.For<IUserRepository>();
        _otpRepo = Substitute.For<IOtpRepository>();
        _handler = new VerifyPasswordResetOtpHandler(_userRepo, _otpRepo);
    }

    /// <summary>Không có OTP hợp lệ phải ném UnauthorizedAccessException.</summary>
    [Test]
    public async Task HandleAsync_NoValidOtp_ThrowsUnauthorizedAccessException()
    {
        _otpRepo.GetLatestValidAsync(Arg.Any<string>(), OtpPurpose.PasswordReset, Arg.Any<CancellationToken>())
            .Returns((OtpCode?)null);

        Func<Task> act = () => _handler.HandleAsync(new VerifyPasswordResetOtpCommand("test@test.com", "123456"));

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    /// <summary>Mã OTP nhập sai phải ném UnauthorizedAccessException.</summary>
    [Test]
    public async Task HandleAsync_WrongCode_ThrowsUnauthorizedAccessException()
    {
        var otp = OtpCode.Create("test@test.com", OtpPurpose.PasswordReset);
        _otpRepo.GetLatestValidAsync("test@test.com", OtpPurpose.PasswordReset, Arg.Any<CancellationToken>()).Returns(otp);

        Func<Task> act = () => _handler.HandleAsync(new VerifyPasswordResetOtpCommand("test@test.com", "wrong-code"));

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        await _otpRepo.DidNotReceive().UpdateAsync(Arg.Any<OtpCode>(), Arg.Any<CancellationToken>());
    }

    /// <summary>OTP đúng nhưng không tìm thấy user tương ứng phải ném UnauthorizedAccessException
    /// (không tiết lộ chi tiết lỗi khác với OTP sai).</summary>
    [Test]
    public async Task HandleAsync_UserNotFound_ThrowsUnauthorizedAccessException()
    {
        var otp = OtpCode.Create("test@test.com", OtpPurpose.PasswordReset);
        _otpRepo.GetLatestValidAsync("test@test.com", OtpPurpose.PasswordReset, Arg.Any<CancellationToken>()).Returns(otp);
        _userRepo.GetByEmailAsync("test@test.com", Arg.Any<CancellationToken>()).Returns((User?)null);

        Func<Task> act = () => _handler.HandleAsync(new VerifyPasswordResetOtpCommand("test@test.com", otp.Code));

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    /// <summary>Dù user không tồn tại, OTP vẫn phải được đánh dấu đã dùng trước khi ném UnauthorizedAccessException.</summary>
    [Test]
    public async Task HandleAsync_UserNotFound_StillMarksOtpAsUsed()
    {
        var otp = OtpCode.Create("test@test.com", OtpPurpose.PasswordReset);
        _otpRepo.GetLatestValidAsync("test@test.com", OtpPurpose.PasswordReset, Arg.Any<CancellationToken>()).Returns(otp);
        _userRepo.GetByEmailAsync("test@test.com", Arg.Any<CancellationToken>()).Returns((User?)null);

        Func<Task> act = () => _handler.HandleAsync(new VerifyPasswordResetOtpCommand("test@test.com", otp.Code));

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        await _otpRepo.Received(1).UpdateAsync(Arg.Is<OtpCode>(o => o.IsUsed), Arg.Any<CancellationToken>());
        await _userRepo.DidNotReceive().UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Xác thực OTP hợp lệ phải đánh dấu OTP đã dùng, cấp reset token ngắn hạn (10 phút) cho user.</summary>
    [Test]
    public async Task HandleAsync_ValidOtp_MarksUsedAndIssuesResetToken()
    {
        var otp = OtpCode.Create("test@test.com", OtpPurpose.PasswordReset);
        var user = User.Create("patient1", "test@test.com", BCrypt.Net.BCrypt.HashPassword("pass"), "Patient");
        _otpRepo.GetLatestValidAsync("test@test.com", OtpPurpose.PasswordReset, Arg.Any<CancellationToken>()).Returns(otp);
        _userRepo.GetByEmailAsync("test@test.com", Arg.Any<CancellationToken>()).Returns(user);

        var result = await _handler.HandleAsync(new VerifyPasswordResetOtpCommand("test@test.com", otp.Code));

        result.ResetToken.Should().NotBeNullOrEmpty();
        user.PasswordResetToken.Should().Be(result.ResetToken);
        user.PasswordResetTokenExpiry.Should().BeCloseTo(DateTimeOffset.UtcNow.AddMinutes(10), TimeSpan.FromSeconds(5));
        await _otpRepo.Received(1).UpdateAsync(Arg.Is<OtpCode>(o => o.IsUsed), Arg.Any<CancellationToken>());
        await _userRepo.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
    }

    /// <summary>Email nhập hoa/thường và có khoảng trắng phải được chuẩn hóa trước khi tra cứu.</summary>
    [Test]
    public async Task HandleAsync_EmailWithWhitespaceAndUpperCase_NormalizesBeforeLookup()
    {
        var otp = OtpCode.Create("test@test.com", OtpPurpose.PasswordReset);
        var user = User.Create("patient1", "test@test.com", BCrypt.Net.BCrypt.HashPassword("pass"), "Patient");
        _otpRepo.GetLatestValidAsync("test@test.com", OtpPurpose.PasswordReset, Arg.Any<CancellationToken>()).Returns(otp);
        _userRepo.GetByEmailAsync("test@test.com", Arg.Any<CancellationToken>()).Returns(user);

        await _handler.HandleAsync(new VerifyPasswordResetOtpCommand("  Test@Test.COM  ", otp.Code));

        await _userRepo.Received(1).GetByEmailAsync("test@test.com", Arg.Any<CancellationToken>());
    }
}
