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
public class ResendOtpHandlerTests
{
    private IUserRepository _userRepo = null!;
    private IOtpRepository _otpRepo = null!;
    private IEmailService _emailService = null!;
    private ResendOtpHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _userRepo = Substitute.For<IUserRepository>();
        _otpRepo = Substitute.For<IOtpRepository>();
        _emailService = Substitute.For<IEmailService>();
        _handler = new ResendOtpHandler(_userRepo, _otpRepo, _emailService);
    }

    private static User CreateInactivePatient(string email)
    {
        var user = User.Create("patient1", email, BCrypt.Net.BCrypt.HashPassword("pass"), UserRole.Patient);
        user.SetActive(false);
        return user;
    }

    /// <summary>Email không tồn tại trong hệ thống phải ném NotFoundException.</summary>
    [Test]
    public async Task HandleAsync_EmailNotFound_ThrowsNotFoundException()
    {
        _userRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        Func<Task> act = () => _handler.Handle(new ResendOtpCommand("notfound@test.com"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>Tài khoản đã active (đã xác thực) rồi thì không cho gửi lại OTP.</summary>
    [Test]
    public async Task HandleAsync_AccountAlreadyActive_ThrowsConflictException()
    {
        var activeUser = User.Create("patient1", "active@test.com", BCrypt.Net.BCrypt.HashPassword("pass"), UserRole.Patient);
        _userRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(activeUser);

        Func<Task> act = () => _handler.Handle(new ResendOtpCommand("active@test.com"), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    /// <summary>Không phải tài khoản Patient thì không được gửi OTP đăng ký.</summary>
    [Test]
    public async Task HandleAsync_NonPatientRole_ThrowsUnauthorizedAccessException()
    {
        var staff = User.Create("staff1", "staff@test.com", BCrypt.Net.BCrypt.HashPassword("pass"), UserRole.Staff);
        staff.SetActive(false);
        _userRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(staff);

        Func<Task> act = () => _handler.Handle(new ResendOtpCommand("staff@test.com"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    /// <summary>Yêu cầu hợp lệ phải vô hiệu hóa OTP cũ, tạo OTP mới và gửi lại email.</summary>
    [Test]
    public async Task HandleAsync_ValidRequest_InvalidatesOldOtpAndSendsNewOne()
    {
        var patient = CreateInactivePatient("patient@test.com");
        _userRepo.GetByEmailAsync("patient@test.com", Arg.Any<CancellationToken>()).Returns(patient);

        await _handler.Handle(new ResendOtpCommand("patient@test.com"), CancellationToken.None);

        await _otpRepo.Received(1).InvalidateAllAsync("patient@test.com", OtpPurpose.Registration, Arg.Any<CancellationToken>());
        await _otpRepo.Received(1).AddAsync(Arg.Any<OtpCode>(), Arg.Any<CancellationToken>());
        await _emailService.Received(1).SendOtpAsync("patient@test.com", Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
