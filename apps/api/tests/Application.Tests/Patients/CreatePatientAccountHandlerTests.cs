using DentalClinic.API.Application.UseCases.Patients;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Patients;

/// <summary>
/// Lễ tân lập tài khoản hộ bệnh nhân — đường DUY NHẤT sinh tài khoản bệnh nhân sau khi bỏ tự đăng ký.
/// </summary>
[TestFixture]
public class CreatePatientAccountHandlerTests
{
    private IUserRepository _userRepo = null!;
    private IPatientRepository _patientRepo = null!;
    private IEmailService _email = null!;
    private IOtpRepository _otpRepo = null!;
    private OtpCode _otp = null!;
    private CreatePatientAccountHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _userRepo = Substitute.For<IUserRepository>();
        _patientRepo = Substitute.For<IPatientRepository>();
        _email = Substitute.For<IEmailService>();
        _userRepo.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _userRepo.ExistsByUsernameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        // Mặc định: email ĐÃ được xác thực bằng mã hợp lệ — các test khác chỉ quan tâm phần sau đó.
        _otpRepo = Substitute.For<IOtpRepository>();
        _otp = OtpCode.Create("benhnhan@gmail.com", OtpPurpose.PatientAccountEmail);
        _otpRepo.GetLatestValidAsync(Arg.Any<string>(), OtpPurpose.PatientAccountEmail, Arg.Any<CancellationToken>())
            .Returns(_otp);

        _handler = new CreatePatientAccountHandler(
            _userRepo, _patientRepo, _otpRepo, _email,
            Substitute.For<IActivityLogService>(), Substitute.For<ICurrentUserService>());
    }

    private Task<CreatePatientAccountResult> Create(string email = "benhnhan@gmail.com") =>
        _handler.Handle(
            new CreatePatientAccountCommand(
                "Nguyễn Văn A", email, "0901234567", new DateOnly(1990, 1, 1), "Nam", _otp.Code),
            CancellationToken.None);

    [Test]
    public async Task Handle_CreatesActivePatientAccountAndProfile()
    {
        await Create();

        await _userRepo.Received(1).AddAsync(
            Arg.Is<User>(u => u.Role == UserRole.Patient && u.IsActive), Arg.Any<CancellationToken>());
        await _patientRepo.Received(1).AddAsync(Arg.Any<Patient>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Mật khẩu do hệ thống sinh và gửi qua email nên nó nằm trong hộp thư bệnh nhân vĩnh viễn —
    /// phải buộc đổi ngay lần đăng nhập đầu để mật khẩu đó hết giá trị.
    /// </summary>
    [Test]
    public async Task Handle_MarksAccountAsMustChangePassword()
    {
        await Create();

        await _userRepo.Received(1).AddAsync(
            Arg.Is<User>(u => u.MustChangePassword), Arg.Any<CancellationToken>());
    }

    /// <summary>Lễ tân đã xác minh bệnh nhân trực tiếp nên không cần OTP như luồng tự đăng ký cũ.</summary>
    [Test]
    public async Task Handle_ActivatesImmediatelyWithoutOtp()
    {
        await Create();

        await _userRepo.Received(1).AddAsync(Arg.Is<User>(u => u.IsActive), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_EmailsTemporaryPassword()
    {
        await Create();

        await _email.Received(1).SendStaffCredentialsAsync(
            "benhnhan@gmail.com", "Nguyễn Văn A", Arg.Is<string>(p => p.Length >= 8), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Gửi email SAU khi lưu: gửi trước rồi lưu hỏng thì bệnh nhân cầm mật khẩu của một tài khoản
    /// không tồn tại, còn email hỏng thì tài khoản vẫn còn và lễ tân đặt lại mật khẩu được.
    /// </summary>
    [Test]
    public async Task Handle_SavesBeforeSendingEmail()
    {
        await Create();

        Received.InOrder(() =>
        {
            _userRepo.AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
            _patientRepo.AddAsync(Arg.Any<Patient>(), Arg.Any<CancellationToken>());
            _email.SendStaffCredentialsAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        });
    }

    [TestCase("  BenhNhan@Gmail.COM  ", "benhnhan@gmail.com")]
    [TestCase("A.B@Example.com", "a.b@example.com")]
    public async Task Handle_NormalisesEmail(string input, string expected)
    {
        var result = await Create(input);

        result.Email.Should().Be(expected);
    }

    /// <summary>Email đã có tài khoản thì báo lỗi — lễ tân phải tra cứu bệnh nhân thay vì tạo trùng.</summary>
    [Test]
    public async Task Handle_ExistingEmail_ThrowsConflict()
    {
        _userRepo.ExistsByEmailAsync("benhnhan@gmail.com", Arg.Any<CancellationToken>()).Returns(true);

        Func<Task> act = () => Create();

        await act.Should().ThrowAsync<ConflictException>();
        await _userRepo.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await _email.DidNotReceive().SendStaffCredentialsAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Username suy ra từ email; trùng thì phải thêm hậu tố chứ không được ném lỗi.</summary>
    [Test]
    public async Task Handle_UsernameTaken_AppendsSuffix()
    {
        _userRepo.ExistsByUsernameAsync("benhnhan", Arg.Any<CancellationToken>()).Returns(true);

        await Create();

        await _userRepo.Received(1).AddAsync(
            Arg.Is<User>(u => u.Username != null && u.Username.StartsWith("benhnhan_")), Arg.Any<CancellationToken>());
    }

    // ── Xác thực email trước khi cấp tài khoản ────────────────────────────────

    /// <summary>
    /// KHÔNG được tạo tài khoản khi email chưa xác thực. Đây là chốt chặn chính: lễ tân gõ nhầm một
    /// ký tự (gmial.com) mà vẫn tạo được thì mật khẩu bay tới hộp thư người lạ, kèm quyền đăng nhập
    /// vào hồ sơ bệnh án của bệnh nhân thật.
    /// </summary>
    [Test]
    public async Task Handle_NoVerificationIssued_ThrowsAndCreatesNothing()
    {
        _otpRepo.GetLatestValidAsync(Arg.Any<string>(), OtpPurpose.PatientAccountEmail, Arg.Any<CancellationToken>())
            .Returns((OtpCode?)null);

        Func<Task> act = () => Create();

        await act.Should().ThrowAsync<ValidationException>();
        await _userRepo.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await _email.DidNotReceive().SendStaffCredentialsAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WrongVerificationCode_ThrowsAndCreatesNothing()
    {
        Func<Task> act = () => _handler.Handle(
            new CreatePatientAccountCommand(
                "Nguyễn Văn A", "benhnhan@gmail.com", "0901234567", null, "Nam", "000000"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        await _userRepo.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Mỗi lần nhập sai phải được đếm — mã 6 chữ số cho thử vô hạn thì chỉ là khóa 20 bit không chốt.</summary>
    [Test]
    public async Task Handle_WrongCode_CountsFailedAttempt()
    {
        Func<Task> act = () => _handler.Handle(
            new CreatePatientAccountCommand(
                "Nguyễn Văn A", "benhnhan@gmail.com", "0901234567", null, "Nam", "000000"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();

        _otp.AttemptCount.Should().Be(1);
        await _otpRepo.Received(1).UpdateAsync(_otp, Arg.Any<CancellationToken>());
    }

    /// <summary>Sai quá số lần cho phép thì mã chết hẳn, phải xin mã mới thay vì dò tiếp.</summary>
    [Test]
    public void Otp_ExceedingMaxAttempts_InvalidatesCode()
    {
        for (var i = 0; i < OtpCode.MaxAttempts; i++) _otp.RegisterFailedAttempt();

        _otp.IsValid().Should().BeFalse();
        _otp.IsUsed.Should().BeTrue();
    }

    /// <summary>Mã đúng phải bị tiêu ngay sau khi dùng, không cấp được tài khoản thứ hai bằng cùng mã.</summary>
    [Test]
    public async Task Handle_ValidCode_ConsumesIt()
    {
        await Create();

        _otp.IsUsed.Should().BeTrue();
        await _otpRepo.Received(1).UpdateAsync(_otp, Arg.Any<CancellationToken>());
    }

    // ── Bệnh nhân đã có hồ sơ từ luồng đặt lịch tại quầy ──────────────────────

    /// <summary>Dựng hồ sơ bệnh nhân đã đến khám tại quầy nhưng CHƯA có tài khoản đăng nhập.</summary>
    private (User User, Patient Patient) SeedWalkInPatient()
    {
        var user = User.CreateEmployee(null!, UserRole.Patient, "0901234567", "Nguyễn Văn A");
        var patient = Patient.Create(user.Id, new DateOnly(1990, 1, 1), "Nam", phoneNumber: "0901234567");

        _patientRepo.GetByPhoneNumberAsync("0901234567", Arg.Any<CancellationToken>()).Returns(patient);
        _userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        return (user, patient);
    }

    /// <summary>
    /// Bệnh nhân từng khám tại quầy (hồ sơ khóa theo số điện thoại) nay được cấp tài khoản: phải NÂNG
    /// CẤP hồ sơ cũ, tuyệt đối không tạo hồ sơ thứ hai — nếu tạo mới thì lịch sử khám, hóa đơn và
    /// bệnh án cũ đều nằm lại ở hồ sơ kia.
    /// </summary>
    [Test]
    public async Task Handle_PatientAlreadyExistsByPhone_UpgradesInsteadOfCreatingDuplicate()
    {
        var (user, patient) = SeedWalkInPatient();

        var result = await Create();

        result.LinkedExistingPatient.Should().BeTrue();
        result.PatientId.Should().Be(patient.Id);

        await _patientRepo.DidNotReceive().AddAsync(Arg.Any<Patient>(), Arg.Any<CancellationToken>());
        await _userRepo.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await _userRepo.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());

        user.HasAccount.Should().BeTrue();
        user.Email.Should().Be("benhnhan@gmail.com");
        user.MustChangePassword.Should().BeTrue();
    }

    [Test]
    public async Task Handle_UpgradedPatient_StillReceivesTemporaryPassword()
    {
        SeedWalkInPatient();

        await Create();

        await _email.Received(1).SendStaffCredentialsAsync(
            "benhnhan@gmail.com", "Nguyễn Văn A", Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Số điện thoại đã gắn với một tài khoản đăng nhập rồi thì báo lỗi, không cấp chồng.</summary>
    [Test]
    public async Task Handle_PhoneAlreadyHasLoginAccount_ThrowsConflict()
    {
        var existingUser = User.Create("cu", "cu@test.com", "hash", UserRole.Patient, "0901234567", "Nguyễn Văn A");
        var patient = Patient.Create(existingUser.Id, new DateOnly(1990, 1, 1), "Nam", phoneNumber: "0901234567");
        _patientRepo.GetByPhoneNumberAsync("0901234567", Arg.Any<CancellationToken>()).Returns(patient);
        _userRepo.GetByIdAsync(existingUser.Id, Arg.Any<CancellationToken>()).Returns(existingUser);

        Func<Task> act = () => Create();

        await act.Should().ThrowAsync<ConflictException>().WithMessage("*0901234567*");
        await _email.DidNotReceive().SendStaffCredentialsAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
