using DentalClinic.API.Application.UseCases.Booking;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Infrastructure.Persistence;
using DentalClinic.API.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using DentalClinic.API.Application.UseCases.Patients;
using MediatR;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Handlers;

[TestFixture]
public class CreateWalkInAppointmentHandlerTests
{
    private AppDbContext _db = null!;
    private INotificationService _notificationService = null!;
    private CreateWalkInAppointmentHandler _handler = null!;
    private IEmailService _emailService = null!;
    private OtpRepository _otpRepo = null!;
    private Guid _dentistId;
    private Guid _dentistUserId;

    [SetUp]
    public async Task SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _notificationService = Substitute.For<INotificationService>();

        // Bệnh nhân đến lần đầu MÀ CÓ email thì handler ủy thác cho CreatePatientAccountHandler qua
        // ISender — nối tuyến thật để test luồng lập tài khoản chạy đúng như lúc chạy thật.
        _emailService = Substitute.For<IEmailService>();
        _otpRepo = new OtpRepository(_db);
        var createPatientAccount = new CreatePatientAccountHandler(
            new UserRepository(_db), new PatientRepository(_db), _otpRepo, _emailService,
            Substitute.For<IActivityLogService>(), Substitute.For<ICurrentUserService>());

        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<CreatePatientAccountCommand>(), Arg.Any<CancellationToken>())
            .Returns(ci => createPatientAccount.Handle(
                (CreatePatientAccountCommand)ci[0], (CancellationToken)ci[1]));

        _handler = new CreateWalkInAppointmentHandler(
            new AppointmentRepository(_db), new PatientRepository(_db), new UserRepository(_db),
            _notificationService, sender);

        var dentistUser = User.Create("d1", $"d1-{Guid.NewGuid()}@test.com", "hash", UserRole.Dentist, fullName: "BS. Nguyễn Văn Hùng");
        _db.Users.Add(dentistUser);
        var employee = Employee.Create(dentistUser.Id, $"DT-{Guid.NewGuid():N}");
        employee.User = dentistUser;
        var dentist = DentistProfile.Create(employee.Id, "Nha khoa tổng quát", "N/A", 5);
        dentist.Employee = employee;
        _db.Employees.Add(employee);
        _db.DentistProfiles.Add(dentist);
        await _db.SaveChangesAsync();
        _dentistId = dentist.Id;
        _dentistUserId = dentistUser.Id;
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    private CreateWalkInCommand MakeCommand(DateTimeOffset appointmentDate) => new(
        _dentistId,
        appointmentDate,
        "Nguyễn Văn A",
        "0901234567",
        new DateOnly(1990, 1, 1),
        "Nam",
        null,
        null);

    /// <summary>Không cho đặt lịch cho khung giờ đã qua quá 15 phút (vượt quá thời gian ân hạn).</summary>
    [Test]
    public async Task HandleAsync_PastAppointmentDate_Beyond15Minutes_ThrowsValidationException()
    {
        var pastDate = DateTimeOffset.UtcNow.AddMinutes(-20);

        Func<Task> act = async () => await _handler.Handle(MakeCommand(pastDate), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Cho phép đặt lịch cho khung giờ đã bắt đầu nhưng chưa quá 15 phút (trong thời gian ân hạn).</summary>
    [Test]
    public async Task HandleAsync_PastAppointmentDate_Within15Minutes_Succeeds()
    {
        var recentDate = DateTimeOffset.UtcNow.AddMinutes(-5);

        var result = await _handler.Handle(MakeCommand(recentDate), CancellationToken.None);

        result.Status.Should().Be("CheckedIn");
        result.PatientName.Should().Be("Nguyễn Văn A");
    }

    /// <summary>
    /// Bệnh nhân đặt tại quầy là đã có mặt, nên lịch hẹn vào thẳng CheckedIn để lên hàng đợi
    /// ngay — staff không phải check-in lại lần nữa.
    /// </summary>
    [Test]
    public async Task HandleAsync_FutureAppointmentDate_CreatesCheckedInAppointment()
    {
        var futureDate = DateTimeOffset.UtcNow.AddHours(1);

        var result = await _handler.Handle(MakeCommand(futureDate), CancellationToken.None);

        result.Status.Should().Be("CheckedIn");
        result.PatientName.Should().Be("Nguyễn Văn A");
    }

    /// <summary>Bệnh nhân vãng lai vào thẳng hàng đợi nên bác sĩ càng cần được báo ngay — không báo
    /// Staff vì chính nhân viên đang thao tác tại quầy đã biết rõ việc này rồi.</summary>
    [Test]
    public async Task HandleAsync_CreatesWalkIn_NotifiesDentistOnly()
    {
        var futureDate = DateTimeOffset.UtcNow.AddHours(1);

        var result = await _handler.Handle(MakeCommand(futureDate), CancellationToken.None);

        await _notificationService.Received(1).CreateAsync(
            Arg.Is<CreateNotificationRequest>(r =>
                r.UserId == _dentistUserId &&
                r.Type == "appointment" &&
                r.Priority == "high" &&
                r.Body.Contains("Nguyễn Văn A") &&
                r.RelatedEntityId == result.AppointmentId.ToString()),
            Arg.Any<CancellationToken>());
        await _notificationService.DidNotReceive().CreateForMultipleUsersAsync(
            Arg.Any<IEnumerable<Guid>>(), Arg.Any<CreateNotificationRequest>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleAsync_SlotAlreadyBooked_ThrowsConflictException()
    {
        var futureDate = DateTimeOffset.UtcNow.AddHours(1);
        await _handler.Handle(MakeCommand(futureDate), CancellationToken.None);

        Func<Task> act = async () => await _handler.Handle(MakeCommand(futureDate), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    /// <summary>
    /// Khi staff chọn hồ sơ từ ô tra cứu, lịch hẹn phải gắn vào đúng hồ sơ đó — kể cả khi
    /// số điện thoại nhập ở form khác với số đang lưu (bệnh nhân đổi số).
    /// </summary>
    [Test]
    public async Task HandleAsync_WithPatientId_ReusesThatPatientAndUpdatesPhone()
    {
        var user = User.CreateEmployee("existing@test.com", UserRole.Patient, phoneNumber: "0900000001", fullName: "Trần Thị B");
        _db.Users.Add(user);
        var existing = Patient.Create(user.Id, new DateOnly(1985, 5, 20), "Nữ", phoneNumber: "0900000001");
        _db.Patients.Add(existing);
        await _db.SaveChangesAsync();

        var cmd = MakeCommand(DateTimeOffset.UtcNow.AddHours(1)) with { PatientId = existing.Id, PatientPhone = "0988887777" };
        var result = await _handler.Handle(cmd, CancellationToken.None);

        _db.Patients.Should().HaveCount(1);
        var appointment = await _db.Appointments.SingleAsync();
        appointment.PatientId.Should().Be(existing.Id);
        // Handler cập nhật đè FullName bằng tên staff nhập tại quầy (chủ ý — cho phép sửa lỗi
        // chính tả tên bệnh nhân cũ), không giữ nguyên tên cũ đã lưu trước đó.
        result.PatientName.Should().Be(cmd.PatientName);
        (await _db.Patients.SingleAsync()).PhoneNumber.Should().Be("0988887777");
    }

    /// <summary>Không tìm thấy hồ sơ đã chọn (đã bị xoá) thì báo lỗi thay vì âm thầm tạo mới.</summary>
    [Test]
    public async Task HandleAsync_WithUnknownPatientId_ThrowsValidationException()
    {
        var cmd = MakeCommand(DateTimeOffset.UtcNow.AddHours(1)) with { PatientId = Guid.NewGuid() };

        Func<Task> act = async () => await _handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>
    /// Bệnh nhân tạo tại quầy không có tài khoản, số điện thoại chỉ nằm ở Patient.PhoneNumber.
    /// Lần tái khám sau phải khớp lại đúng hồ sơ cũ chứ không sinh bản ghi trùng.
    /// </summary>
    [Test]
    public async Task HandleAsync_SamePhoneAsAccountlessPatient_ReusesExistingPatient()
    {
        await _handler.Handle(MakeCommand(DateTimeOffset.UtcNow.AddHours(1)), CancellationToken.None);

        await _handler.Handle(MakeCommand(DateTimeOffset.UtcNow.AddHours(2)), CancellationToken.None);

        _db.Patients.Should().HaveCount(1);
        _db.Appointments.Should().HaveCount(2);
    }

    /// <summary>
    /// Lịch hẹn cũ tại đúng khung giờ đó đã bị hủy (Cancelled) không được coi là chiếm slot —
    /// staff phải đặt lại được bình thường cho bệnh nhân khác vào đúng giờ đó.
    /// </summary>
    [Test]
    public async Task HandleAsync_SlotOnlyHasCancelledAppointment_AllowsBooking()
    {
        var futureDate = DateTimeOffset.UtcNow.AddHours(1);
        var cancelledPatient = Patient.Create(Guid.Empty, new DateOnly(1988, 3, 3), "Nữ", phoneNumber: "0900000009");
        _db.Patients.Add(cancelledPatient);
        var cancelledAppointment = Appointment.Create(cancelledPatient.Id, _dentistId, futureDate);
        cancelledAppointment.Cancel(CancellationReason.ChangeOfPlans, null, cancelledByUserId: null, DateTimeOffset.UtcNow);
        _db.Appointments.Add(cancelledAppointment);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(MakeCommand(futureDate), CancellationToken.None);

        result.Status.Should().Be("CheckedIn");
        _db.Appointments.Should().HaveCount(2);
    }

    // ── Gộp luồng lập tài khoản vào đặt lịch tại quầy ─────────────────────────

    /// <summary>Gửi mã xác thực cho email rồi trả lại mã — mô phỏng bước bệnh nhân đọc mã cho lễ tân.</summary>
    private async Task<string> IssueVerificationCodeAsync(string email)
    {
        var otp = OtpCode.Create(email, OtpPurpose.PatientAccountEmail);
        await _otpRepo.AddAsync(otp);
        return otp.Code;
    }

    /// <summary>
    /// Bệnh nhân đến lần đầu có email ĐÃ XÁC THỰC ⇒ lập luôn TÀI KHOẢN THẬT (đăng nhập được, buộc
    /// đổi mật khẩu) để lần sau họ tự đặt lịch trên app.
    /// </summary>
    [Test]
    public async Task HandleAsync_NewPatientWithVerifiedEmail_CreatesRealLoginAccount()
    {
        var code = await IssueVerificationCodeAsync("moi@gmail.com");
        var cmd = MakeCommand(DateTimeOffset.UtcNow.AddHours(1))
            with { PatientEmail = "moi@gmail.com", EmailVerificationCode = code };

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.Status.Should().Be("CheckedIn");

        var created = await _db.Users.SingleAsync(u => u.Email == "moi@gmail.com");
        created.Role.Should().Be(UserRole.Patient);
        created.HasAccount.Should().BeTrue("có email thì phải lập tài khoản đăng nhập được");
        created.MustChangePassword.Should().BeTrue();
        created.IsActive.Should().BeTrue("lễ tân đã xác minh trực tiếp nên không cần OTP");

        await _emailService.Received(1).SendStaffCredentialsAsync(
            "moi@gmail.com", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Không có email vẫn phải khám được — người lớn tuổi thường không dùng email. Tạo hồ sơ không
    /// tài khoản như trước, họ chỉ chưa dùng được app cho tới khi cung cấp email.
    /// </summary>
    [Test]
    public async Task HandleAsync_NewPatientWithoutEmail_CreatesProfileWithoutAccount()
    {
        var result = await _handler.Handle(MakeCommand(DateTimeOffset.UtcNow.AddHours(1)), CancellationToken.None);

        result.Status.Should().Be("CheckedIn");

        var appointment = await _db.Appointments.SingleAsync();
        var patient = await _db.Patients.SingleAsync(p => p.Id == appointment.PatientId);
        var user = await _db.Users.SingleAsync(u => u.Id == patient.UserId);

        user.HasAccount.Should().BeFalse("không có email thì không lập tài khoản đăng nhập");
        await _emailService.DidNotReceive().SendStaffCredentialsAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Bệnh nhân đã có hồ sơ (khớp số điện thoại) thì đi thẳng vào đặt lịch, không lập tài khoản lần
    /// hai — kể cả khi lễ tân có nhập email.
    /// </summary>
    [Test]
    public async Task HandleAsync_ExistingPatient_DoesNotCreateAnotherAccount()
    {
        var user = User.CreateEmployee("cu@test.com", UserRole.Patient, "0901234567", "Nguyễn Văn A");
        _db.Users.Add(user);
        _db.Patients.Add(Patient.Create(user.Id, new DateOnly(1990, 1, 1), "Nam", phoneNumber: "0901234567"));
        await _db.SaveChangesAsync();

        var code = await IssueVerificationCodeAsync("moi@gmail.com");
        var cmd = MakeCommand(DateTimeOffset.UtcNow.AddHours(1))
            with { PatientEmail = "moi@gmail.com", EmailVerificationCode = code };
        await _handler.Handle(cmd, CancellationToken.None);

        _db.Patients.Should().HaveCount(1);
        (await _db.Users.CountAsync(u => u.Email == "moi@gmail.com")).Should().Be(0);
        await _emailService.DidNotReceive().SendStaffCredentialsAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ── Check-in tái khám (FollowUpFromAppointmentId) ─────────────────────────
    // Thay cho CheckInFollowUpHandler cũ: staff giờ đặt lịch tại quầy bình thường (chọn giờ/bác sĩ
    // còn ca trên lưới) kèm FollowUpFromAppointmentId, thay vì tự động gán thẳng vào bác sĩ cũ dù
    // có ca hay không.

    /// <summary>Buổi hẹn gốc không tồn tại phải báo lỗi thay vì âm thầm bỏ qua liên kết.</summary>
    [Test]
    public async Task HandleAsync_FollowUpFromAppointmentNotFound_ThrowsNotFoundException()
    {
        var cmd = MakeCommand(DateTimeOffset.UtcNow.AddHours(1)) with { FollowUpFromAppointmentId = Guid.NewGuid() };

        Func<Task> act = async () => await _handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>Buổi hẹn gốc chưa được bác sĩ hẹn tái khám (FollowUpDate null) thì không cho check-in.</summary>
    [Test]
    public async Task HandleAsync_FollowUpFromAppointmentWithoutFollowUpDate_ThrowsValidationException()
    {
        var patient = Patient.Create(Guid.Empty, new DateOnly(1990, 1, 1), "Nam", phoneNumber: "0900000005");
        _db.Patients.Add(patient);
        var original = Appointment.Create(patient.Id, _dentistId, DateTimeOffset.UtcNow.AddDays(-10));
        original.Complete();
        _db.Appointments.Add(original);
        await _db.SaveChangesAsync();

        var cmd = MakeCommand(DateTimeOffset.UtcNow.AddHours(1)) with { FollowUpFromAppointmentId = original.Id };
        Func<Task> act = async () => await _handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Buổi gốc đã có buổi tái khám check-in rồi (chưa hủy) thì không cho check-in lần 2.</summary>
    [Test]
    public async Task HandleAsync_FollowUpAlreadyCheckedIn_ThrowsConflictException()
    {
        var patient = Patient.Create(Guid.Empty, new DateOnly(1990, 1, 1), "Nam", phoneNumber: "0900000006");
        _db.Patients.Add(patient);
        var original = Appointment.Create(patient.Id, _dentistId, DateTimeOffset.UtcNow.AddDays(-10));
        original.Complete();
        original.SetFollowUpReminder(DateOnly.FromDateTime(DateTime.Today.AddDays(1)), null);
        _db.Appointments.Add(original);
        await _db.SaveChangesAsync();
        var alreadyCheckedIn = Appointment.CreateWalkIn(patient.Id, _dentistId, DateTimeOffset.UtcNow, followUpFromAppointmentId: original.Id);
        _db.Appointments.Add(alreadyCheckedIn);
        await _db.SaveChangesAsync();

        var cmd = MakeCommand(DateTimeOffset.UtcNow.AddHours(1)) with { FollowUpFromAppointmentId = original.Id };
        Func<Task> act = async () => await _handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    /// <summary>Check-in tái khám hợp lệ phải tạo buổi hẹn mới CheckedIn, gắn về đúng buổi gốc —
    /// kể cả khi staff chọn giờ/bác sĩ khác với buổi gốc trên lưới đặt lịch.</summary>
    [Test]
    public async Task HandleAsync_ValidFollowUpFromAppointmentId_CreatesLinkedCheckedInAppointment()
    {
        var patient = Patient.Create(Guid.Empty, new DateOnly(1990, 1, 1), "Nam", phoneNumber: "0900000007");
        _db.Patients.Add(patient);
        var original = Appointment.Create(patient.Id, _dentistId, DateTimeOffset.UtcNow.AddDays(-10));
        original.Complete();
        original.SetFollowUpReminder(DateOnly.FromDateTime(DateTime.Today.AddDays(1)), null);
        _db.Appointments.Add(original);
        await _db.SaveChangesAsync();

        var cmd = MakeCommand(DateTimeOffset.UtcNow.AddHours(1)) with { FollowUpFromAppointmentId = original.Id };
        var result = await _handler.Handle(cmd, CancellationToken.None);

        var followUp = await _db.Appointments.SingleAsync(a => a.Id == result.AppointmentId);
        followUp.Status.Should().Be(AppointmentStatus.CheckedIn);
        followUp.FollowUpFromAppointmentId.Should().Be(original.Id);
    }
}
