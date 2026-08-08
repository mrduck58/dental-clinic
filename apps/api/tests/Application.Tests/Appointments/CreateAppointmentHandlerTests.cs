using DentalClinic.API.Application.UseCases.Booking;
using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Appointments;

[TestFixture]
public class CreateAppointmentHandlerTests
{
    private IAppointmentRepository _appointmentRepo = null!;
    private IPatientRepository _patientRepo = null!;
    private IUserRepository _userRepo = null!;
    private IServiceRepository _serviceRepo = null!;
    private AppointmentSlotGuard _slotGuard = null!;
    private INotificationService _notification = null!;
    private CreateAppointmentHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _appointmentRepo = Substitute.For<IAppointmentRepository>();
        _patientRepo = Substitute.For<IPatientRepository>();
        _userRepo = Substitute.For<IUserRepository>();
        _serviceRepo = Substitute.For<IServiceRepository>();
        _notification = Substitute.For<INotificationService>();
        _slotGuard = new AppointmentSlotGuard(_appointmentRepo, _serviceRepo);
        _handler = new CreateAppointmentHandler(_appointmentRepo, _patientRepo, _userRepo, _slotGuard, _notification);

        // Mặc định: không có lịch hẹn nào khác trong ngày, không có dentist user, không có staff
        _appointmentRepo.GetByDateAsync(Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new List<Appointment>());
        _appointmentRepo.GetDentistUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Guid?)null);
        _userRepo.GetUserIdsByRoleAsync("Staff", Arg.Any<CancellationToken>())
            .Returns(new List<Guid>());
    }

    /// <summary>
    /// Khi bệnh nhân đã có hồ sơ Patient, AddAsync của patientRepo không được gọi —
    /// gọi lại sẽ tạo hồ sơ trùng lặp và vi phạm ràng buộc unique UserId.
    /// </summary>
    [Test]
    public async Task HandleAsync_PatientAlreadyExists_DoesNotCreateNewPatient()
    {
        var cmd = MakeCmd();
        var existingPatient = Patient.Create(cmd.UserId, DateOnly.FromDateTime(DateTime.Today.AddYears(-20)), "Nam");
        _patientRepo.GetByUserIdAsync(cmd.UserId, Arg.Any<CancellationToken>()).Returns(existingPatient);

        await _handler.Handle(cmd, CancellationToken.None);

        await _patientRepo.DidNotReceive().AddAsync(Arg.Any<Patient>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Khi bệnh nhân chưa có hồ sơ Patient, handler phải tạo mới và gọi AddAsync đúng 1 lần —
    /// bỏ qua bước này sẽ khiến appointment không liên kết được với hồ sơ bệnh nhân.
    /// </summary>
    [Test]
    public async Task HandleAsync_PatientNotExists_CreatesNewPatient()
    {
        var cmd = MakeCmd();
        var user = User.Create("patient01", "patient@test.com", "hash", UserRole.Patient, null, "Trần Thị B");
        _patientRepo.GetByUserIdAsync(cmd.UserId, Arg.Any<CancellationToken>()).Returns((Patient?)null);
        _userRepo.GetByIdAsync(cmd.UserId, Arg.Any<CancellationToken>()).Returns(user);

        await _handler.Handle(cmd, CancellationToken.None);

        await _patientRepo.Received(1).AddAsync(Arg.Any<Patient>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Khi khung giờ đã được đặt, handler phải ném ConflictException trước khi lưu appointment —
    /// cho phép đặt lịch chồng sẽ gây xung đột lịch làm việc cho nha sĩ.
    /// </summary>
    [Test]
    public async Task HandleAsync_SlotAlreadyBooked_ThrowsConflictException()
    {
        var cmd = MakeCmd();
        var existingPatient = Patient.Create(cmd.UserId, DateOnly.FromDateTime(DateTime.Today.AddYears(-20)), "Nam");
        _patientRepo.GetByUserIdAsync(cmd.UserId, Arg.Any<CancellationToken>()).Returns(existingPatient);
        // Lịch hẹn khác đã chiếm đúng khung giờ này của cùng nha sĩ → chồng lấn.
        var conflictingAppointment = Appointment.Create(Guid.NewGuid(), cmd.DentistId, cmd.AppointmentDate);
        _appointmentRepo.GetByDateAsync(Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new List<Appointment> { conflictingAppointment });

        Func<Task> act = () => _handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    /// <summary>
    /// Với dữ liệu hợp lệ, appointmentRepository.AddAsync phải được gọi đúng 1 lần —
    /// không gọi thì appointment không được lưu vào DB.
    /// </summary>
    [Test]
    public async Task HandleAsync_ValidCommand_CallsAppointmentAddAsync()
    {
        var cmd = MakeCmd();
        var existingPatient = Patient.Create(cmd.UserId, DateOnly.FromDateTime(DateTime.Today.AddYears(-20)), "Nam");
        _patientRepo.GetByUserIdAsync(cmd.UserId, Arg.Any<CancellationToken>()).Returns(existingPatient);

        await _handler.Handle(cmd, CancellationToken.None);

        await _appointmentRepo.Received(1).AddAsync(Arg.Any<Appointment>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Lịch hẹn mới tạo luôn phải ở trạng thái "Pending" để chờ nhân viên xác nhận,
    /// trả về trạng thái khác sẽ làm sai luồng nghiệp vụ xác nhận lịch.
    /// </summary>
    [Test]
    public async Task HandleAsync_ValidCommand_ReturnsPendingStatus()
    {
        var cmd = MakeCmd();
        var existingPatient = Patient.Create(cmd.UserId, DateOnly.FromDateTime(DateTime.Today.AddYears(-20)), "Nam");
        _patientRepo.GetByUserIdAsync(cmd.UserId, Arg.Any<CancellationToken>()).Returns(existingPatient);

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.Status.Should().Be("Pending");
    }

    /// <summary>
    /// Khi tìm thấy userId của nha sĩ, notificationService.CreateAsync phải được gọi với Type = Appointment —
    /// nha sĩ cần được thông báo ngay khi có lịch hẹn mới để chuẩn bị.
    /// </summary>
    [Test]
    public async Task HandleAsync_ValidCommand_NotifiesDentistUser()
    {
        var cmd = MakeCmd();
        var existingPatient = Patient.Create(cmd.UserId, DateOnly.FromDateTime(DateTime.Today.AddYears(-20)), "Nam");
        var dentistUserId = Guid.NewGuid();
        _patientRepo.GetByUserIdAsync(cmd.UserId, Arg.Any<CancellationToken>()).Returns(existingPatient);
        _appointmentRepo.GetDentistUserIdAsync(cmd.DentistId, Arg.Any<CancellationToken>()).Returns(dentistUserId);

        await _handler.Handle(cmd, CancellationToken.None);

        // Lọc thêm theo UserId của bác sĩ — handler còn gửi 1 thông báo khác cho chính bệnh nhân
        // (cũng Type=Appointment) nên chỉ lọc theo Type sẽ khớp nhầm cả 2, ra "received 2" thay vì 1.
        await _notification.Received(1).CreateAsync(
            Arg.Is<CreateNotificationRequest>(r => r.Type == NotificationType.Appointment && r.UserId == dentistUserId),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Khi có 2 nhân viên Staff, CreateForMultipleUsersAsync phải được gọi 1 lần với đủ 2 ID —
    /// bỏ sót ID thì nhân viên không nhận được thông báo, gọi nhiều lần thì nhận thông báo trùng.
    /// </summary>
    [Test]
    public async Task HandleAsync_ValidCommand_NotifiesAllStaff()
    {
        var cmd = MakeCmd();
        var existingPatient = Patient.Create(cmd.UserId, DateOnly.FromDateTime(DateTime.Today.AddYears(-20)), "Nam");
        var staff1 = Guid.NewGuid();
        var staff2 = Guid.NewGuid();
        _patientRepo.GetByUserIdAsync(cmd.UserId, Arg.Any<CancellationToken>()).Returns(existingPatient);
        _userRepo.GetUserIdsByRoleAsync("Staff", Arg.Any<CancellationToken>())
            .Returns(new List<Guid> { staff1, staff2 });

        await _handler.Handle(cmd, CancellationToken.None);

        await _notification.Received(1).CreateForMultipleUsersAsync(
            Arg.Is<IEnumerable<Guid>>(ids => ids.Contains(staff1) && ids.Contains(staff2)),
            Arg.Any<CreateNotificationRequest>(),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Khi bệnh nhân chưa có hồ sơ Patient và tài khoản User cũng không tồn tại, handler phải ném
    /// InvalidOperationException — không thể tạo hồ sơ Patient từ một tài khoản không có thật.
    /// </summary>
    [Test]
    public async Task HandleAsync_PatientNotExistsAndUserNotFound_ThrowsInvalidOperationException()
    {
        var cmd = MakeCmd();
        _patientRepo.GetByUserIdAsync(cmd.UserId, Arg.Any<CancellationToken>()).Returns((Patient?)null);
        _userRepo.GetByIdAsync(cmd.UserId, Arg.Any<CancellationToken>()).Returns((User?)null);

        Func<Task> act = () => _handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    /// <summary>
    /// Khi PatientId trỏ đến một hồ sơ hợp lệ thuộc gia đình (PrimaryPatientId đúng), lịch hẹn phải
    /// gắn vào hồ sơ thành viên đó chứ không phải hồ sơ chính — để đúng người được đặt lịch.
    /// </summary>
    [Test]
    public async Task HandleAsync_ValidFamilyMemberPatientId_BooksAppointmentForFamilyMember()
    {
        var primaryPatient = Patient.Create(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.Today.AddYears(-40)), "Nam");
        var familyMember = Patient.Create(Guid.Empty, DateOnly.FromDateTime(DateTime.Today.AddYears(-10)), "Nam", primaryPatientId: primaryPatient.Id);
        var cmd = MakeCmd() with { UserId = primaryPatient.UserId, PatientId = familyMember.Id };
        _patientRepo.GetByUserIdAsync(cmd.UserId, Arg.Any<CancellationToken>()).Returns(primaryPatient);
        _patientRepo.GetByIdAsync(familyMember.Id, Arg.Any<CancellationToken>()).Returns(familyMember);

        await _handler.Handle(cmd, CancellationToken.None);

        await _appointmentRepo.Received(1).AddAsync(
            Arg.Is<Appointment>(a => a.PatientId == familyMember.Id), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Khi PatientId không thuộc gia đình của người dùng hiện tại (hoặc không tồn tại), handler phải
    /// ném InvalidOperationException — chặn đặt lịch hộ hồ sơ của người khác.
    /// </summary>
    [Test]
    public async Task HandleAsync_PatientIdNotBelongingToFamily_ThrowsInvalidOperationException()
    {
        var primaryPatient = Patient.Create(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.Today.AddYears(-40)), "Nam");
        var otherPatient = Patient.Create(Guid.Empty, DateOnly.FromDateTime(DateTime.Today.AddYears(-30)), "Nữ");
        var cmd = MakeCmd() with { UserId = primaryPatient.UserId, PatientId = otherPatient.Id };
        _patientRepo.GetByUserIdAsync(cmd.UserId, Arg.Any<CancellationToken>()).Returns(primaryPatient);
        _patientRepo.GetByIdAsync(otherPatient.Id, Arg.Any<CancellationToken>()).Returns(otherPatient);

        Func<Task> act = () => _handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    /// <summary>
    /// Khi không tìm được userId của nha sĩ (chưa có tài khoản liên kết), handler không được gọi
    /// notificationService.CreateAsync — tránh gửi thông báo cho UserId rỗng.
    /// </summary>
    [Test]
    public async Task HandleAsync_DentistUserIdNotFound_DoesNotSendDentistNotification()
    {
        var cmd = MakeCmd();
        var existingPatient = Patient.Create(cmd.UserId, DateOnly.FromDateTime(DateTime.Today.AddYears(-20)), "Nam");
        _patientRepo.GetByUserIdAsync(cmd.UserId, Arg.Any<CancellationToken>()).Returns(existingPatient);
        _appointmentRepo.GetDentistUserIdAsync(cmd.DentistId, Arg.Any<CancellationToken>()).Returns((Guid?)null);

        await _handler.Handle(cmd, CancellationToken.None);

        // Không tìm được UserId của bác sĩ thì chỉ bỏ qua thông báo CHO BÁC SĨ ("Lịch hẹn mới") — bệnh
        // nhân vẫn phải nhận thông báo xác nhận đặt lịch như thường, nên không thể assert chung
        // "không gửi thông báo nào" (handler vẫn gọi CreateAsync 1 lần cho bệnh nhân).
        await _notification.DidNotReceive().CreateAsync(
            Arg.Is<CreateNotificationRequest>(r => r.Title == "Lịch hẹn mới"),
            Arg.Any<CancellationToken>());
    }

    private static CreateAppointmentCommand MakeCmd() =>
        new(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(1), null, null, null);
}
