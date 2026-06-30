using DentalClinic.API.Application.UseCases.Appointments;
using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Entities;
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
    private INotificationService _notification = null!;
    private CreateAppointmentHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _appointmentRepo = Substitute.For<IAppointmentRepository>();
        _patientRepo = Substitute.For<IPatientRepository>();
        _userRepo = Substitute.For<IUserRepository>();
        _notification = Substitute.For<INotificationService>();
        _handler = new CreateAppointmentHandler(_appointmentRepo, _patientRepo, _userRepo, _notification);

        // Mặc định: slot trống, không có dentist user, không có staff
        _appointmentRepo.IsSlotBookedAsync(Arg.Any<Guid>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(false);
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
        var existingPatient = Patient.Create("Nguyễn Văn A", DateOnly.FromDateTime(DateTime.Today.AddYears(-20)), "Nam", cmd.UserId);
        _patientRepo.GetByUserIdAsync(cmd.UserId, Arg.Any<CancellationToken>()).Returns(existingPatient);

        await _handler.HandleAsync(cmd);

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
        var user = User.Create("patient01", "patient@test.com", "hash", "Patient", null, "Trần Thị B");
        _patientRepo.GetByUserIdAsync(cmd.UserId, Arg.Any<CancellationToken>()).Returns((Patient?)null);
        _userRepo.GetByIdAsync(cmd.UserId, Arg.Any<CancellationToken>()).Returns(user);

        await _handler.HandleAsync(cmd);

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
        var existingPatient = Patient.Create("Test", DateOnly.FromDateTime(DateTime.Today.AddYears(-20)), "Nam", cmd.UserId);
        _patientRepo.GetByUserIdAsync(cmd.UserId, Arg.Any<CancellationToken>()).Returns(existingPatient);
        _appointmentRepo.IsSlotBookedAsync(cmd.DentistId, cmd.AppointmentDate, Arg.Any<CancellationToken>())
            .Returns(true);

        Func<Task> act = () => _handler.HandleAsync(cmd);

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
        var existingPatient = Patient.Create("Test", DateOnly.FromDateTime(DateTime.Today.AddYears(-20)), "Nam", cmd.UserId);
        _patientRepo.GetByUserIdAsync(cmd.UserId, Arg.Any<CancellationToken>()).Returns(existingPatient);

        await _handler.HandleAsync(cmd);

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
        var existingPatient = Patient.Create("Test", DateOnly.FromDateTime(DateTime.Today.AddYears(-20)), "Nam", cmd.UserId);
        _patientRepo.GetByUserIdAsync(cmd.UserId, Arg.Any<CancellationToken>()).Returns(existingPatient);

        var result = await _handler.HandleAsync(cmd);

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
        var existingPatient = Patient.Create("Test", DateOnly.FromDateTime(DateTime.Today.AddYears(-20)), "Nam", cmd.UserId);
        var dentistUserId = Guid.NewGuid();
        _patientRepo.GetByUserIdAsync(cmd.UserId, Arg.Any<CancellationToken>()).Returns(existingPatient);
        _appointmentRepo.GetDentistUserIdAsync(cmd.DentistId, Arg.Any<CancellationToken>()).Returns(dentistUserId);

        await _handler.HandleAsync(cmd);

        await _notification.Received(1).CreateAsync(
            Arg.Is<CreateNotificationRequest>(r => r.Type == NotificationType.Appointment),
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
        var existingPatient = Patient.Create("Test", DateOnly.FromDateTime(DateTime.Today.AddYears(-20)), "Nam", cmd.UserId);
        var staff1 = Guid.NewGuid();
        var staff2 = Guid.NewGuid();
        _patientRepo.GetByUserIdAsync(cmd.UserId, Arg.Any<CancellationToken>()).Returns(existingPatient);
        _userRepo.GetUserIdsByRoleAsync("Staff", Arg.Any<CancellationToken>())
            .Returns(new List<Guid> { staff1, staff2 });

        await _handler.HandleAsync(cmd);

        await _notification.Received(1).CreateForMultipleUsersAsync(
            Arg.Is<IEnumerable<Guid>>(ids => ids.Contains(staff1) && ids.Contains(staff2)),
            Arg.Any<CreateNotificationRequest>(),
            Arg.Any<CancellationToken>());
    }

    private static CreateAppointmentCommand MakeCmd() =>
        new(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(1), null, null);
}
