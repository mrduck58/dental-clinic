using DentalClinic.API.Application.UseCases.Appointments;
using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Appointments;

[TestFixture]
public class UpdateAppointmentStatusHandlerTests
{
    private IAppointmentRepository _repo = null!;
    private IActivityLogService _activityLog = null!;
    private ICurrentUserService _currentUser = null!;
    private INotificationService _notification = null!;
    private IPatientRepository _patientRepo = null!;
    private UpdateAppointmentStatusHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = Substitute.For<IAppointmentRepository>();
        _activityLog = Substitute.For<IActivityLogService>();
        _currentUser = Substitute.For<ICurrentUserService>();
        _notification = Substitute.For<INotificationService>();
        _patientRepo = Substitute.For<IPatientRepository>();
        _handler = new UpdateAppointmentStatusHandler(_repo, _activityLog, _notification, _currentUser, _patientRepo);
    }

    private static Appointment MakeAppointment() =>
        Appointment.Create(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(1));

    // ── ConfirmAsync ──────────────────────────────────────────────────────────

    /// <summary>
    /// Xác nhận lịch hẹn tồn tại phải gọi UpdateAsync đúng 1 lần để lưu trạng thái mới,
    /// gọi 0 lần sẽ không persist được thay đổi, gọi nhiều lần sẽ sinh race condition.
    /// </summary>
    [Test]
    public async Task ConfirmAsync_ExistingAppointment_CallsUpdateAsyncOnce()
    {
        var id = Guid.NewGuid();
        var appt = MakeAppointment();
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(appt);

        await _handler.ConfirmAsync(id);

        await _repo.Received(1).UpdateAsync(appt, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Sau khi xác nhận, trạng thái lịch hẹn phải chuyển sang Confirmed,
    /// để bệnh nhân nhận được thông báo đúng trạng thái trên app.
    /// </summary>
    [Test]
    public async Task ConfirmAsync_ExistingAppointment_SetsStatusToConfirmed()
    {
        var id = Guid.NewGuid();
        var appt = MakeAppointment();
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(appt);

        await _handler.ConfirmAsync(id);

        appt.Status.Should().Be(AppointmentStatus.Confirmed);
    }

    /// <summary>
    /// Lịch hẹn mới tạo luôn ở trạng thái Pending, ConfirmAsync không được bỏ qua bước này
    /// và phải thực sự thay đổi status thay vì chỉ gọi Update mà không mutate entity.
    /// </summary>
    [Test]
    public async Task ConfirmAsync_ExistingAppointment_StatusWasPendingBeforeConfirm()
    {
        var id = Guid.NewGuid();
        var appt = MakeAppointment();
        appt.Status.Should().Be(AppointmentStatus.Pending); // trạng thái ban đầu

        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(appt);
        await _handler.ConfirmAsync(id);

        appt.Status.Should().Be(AppointmentStatus.Confirmed);
    }

    /// <summary>
    /// appointmentId không tồn tại phải ném KeyNotFoundException với message chứa id,
    /// để controller tra về 404 và log đúng id bị thiếu để debug.
    /// </summary>
    [Test]
    public async Task ConfirmAsync_NonExistentAppointment_ThrowsKeyNotFoundException()
    {
        var id = Guid.NewGuid();
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Appointment?)null);

        Func<Task> act = () => _handler.ConfirmAsync(id);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*{id}*");
    }

    /// <summary>
    /// Khi lịch hẹn không tìm thấy, UpdateAsync không được gọi để tránh lưu entity rỗng vào DB.
    /// </summary>
    [Test]
    public async Task ConfirmAsync_NonExistentAppointment_DoesNotCallUpdateAsync()
    {
        var id = Guid.NewGuid();
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Appointment?)null);

        Assert.CatchAsync(() => _handler.ConfirmAsync(id));

        await _repo.DidNotReceive().UpdateAsync(Arg.Any<Appointment>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Sau khi xác nhận, nha sĩ phụ trách phải nhận thông báo loại Appointment —
    /// nếu không gửi, nha sĩ sẽ không biết lịch hẹn đã được xác nhận.
    /// </summary>
    [Test]
    public async Task ConfirmAsync_ExistingAppointment_SendsNotificationToDentist()
    {
        var id = Guid.NewGuid();
        var appt = MakeAppointment();
        var dentistUserId = Guid.NewGuid();
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(appt);
        _repo.GetDentistUserIdAsync(appt.DentistId, Arg.Any<CancellationToken>()).Returns(dentistUserId);

        await _handler.ConfirmAsync(id);

        await _notification.Received(1).CreateAsync(
            Arg.Is<CreateNotificationRequest>(r => r.Type == NotificationType.Appointment),
            Arg.Any<CancellationToken>());
    }

    // ── CancelAsync ───────────────────────────────────────────────────────────

    /// <summary>
    /// Hủy lịch hẹn tồn tại phải gọi UpdateAsync đúng 1 lần để persist trạng thái Cancelled.
    /// </summary>
    [Test]
    public async Task CancelAsync_ExistingAppointment_CallsUpdateAsyncOnce()
    {
        var id = Guid.NewGuid();
        var appt = MakeAppointment();
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(appt);

        await _handler.CancelAsync(id);

        await _repo.Received(1).UpdateAsync(appt, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Sau khi hủy, trạng thái lịch hẹn phải chuyển sang Cancelled,
    /// để bệnh nhân không thể vào khám theo lịch đã bị hủy.
    /// </summary>
    [Test]
    public async Task CancelAsync_ExistingAppointment_SetsStatusToCancelled()
    {
        var id = Guid.NewGuid();
        var appt = MakeAppointment();
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(appt);

        await _handler.CancelAsync(id);

        appt.Status.Should().Be(AppointmentStatus.Cancelled);
    }

    /// <summary>
    /// appointmentId không tồn tại khi hủy phải ném KeyNotFoundException với message chứa id.
    /// </summary>
    [Test]
    public async Task CancelAsync_NonExistentAppointment_ThrowsKeyNotFoundException()
    {
        var id = Guid.NewGuid();
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Appointment?)null);

        Func<Task> act = () => _handler.CancelAsync(id);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*{id}*");
    }

    /// <summary>
    /// Khi lịch hẹn không tìm thấy để hủy, UpdateAsync không được gọi.
    /// </summary>
    [Test]
    public async Task CancelAsync_NonExistentAppointment_DoesNotCallUpdateAsync()
    {
        var id = Guid.NewGuid();
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Appointment?)null);

        Assert.CatchAsync(() => _handler.CancelAsync(id));

        await _repo.DidNotReceive().UpdateAsync(Arg.Any<Appointment>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Khi hủy lịch hẹn, nha sĩ phải nhận thông báo ưu tiên High vì đây là thay đổi quan trọng
    /// ảnh hưởng trực tiếp đến lịch làm việc của nha sĩ.
    /// </summary>
    [Test]
    public async Task CancelAsync_ExistingAppointment_SendsHighPriorityNotificationToDentist()
    {
        var id = Guid.NewGuid();
        var appt = MakeAppointment();
        var dentistUserId = Guid.NewGuid();
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(appt);
        _repo.GetDentistUserIdAsync(appt.DentistId, Arg.Any<CancellationToken>()).Returns(dentistUserId);

        await _handler.CancelAsync(id);

        await _notification.Received(1).CreateAsync(
            Arg.Is<CreateNotificationRequest>(r => r.Priority == NotificationPriority.High),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Bệnh nhân hủy lịch hẹn của chính mình thành công.
    /// </summary>
    [Test]
    public async Task CancelAsync_PatientCancelsOwnAppointment_Succeeds()
    {
        var id = Guid.NewGuid();
        var patientUserId = Guid.NewGuid();
        var patient = Patient.Create("Test Patient", new DateOnly(1990, 1, 1), "Nam", patientUserId);
        var appt = Appointment.Create(patient.Id, Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(1));

        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(patientUserId);
        _currentUser.UserRole.Returns("Patient");

        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(appt);
        _patientRepo.GetByUserIdAsync(patientUserId, Arg.Any<CancellationToken>()).Returns(patient);

        await _handler.CancelAsync(id, "Bận việc đột xuất");

        appt.Status.Should().Be(AppointmentStatus.Cancelled);
        appt.Notes.Should().Contain("Bận việc đột xuất");
        await _repo.Received(1).UpdateAsync(appt, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Bệnh nhân hủy lịch hẹn của người thân (family member) thành công.
    /// </summary>
    [Test]
    public async Task CancelAsync_PatientCancelsFamilyMemberAppointment_Succeeds()
    {
        var id = Guid.NewGuid();
        var patientUserId = Guid.NewGuid();
        var patient = Patient.Create("Primary Patient", new DateOnly(1990, 1, 1), "Nam", patientUserId);
        var familyMember = Patient.Create("Family Member", new DateOnly(2015, 5, 5), "Nam", primaryPatientId: patient.Id, relationship: "Con trai");
        var appt = Appointment.Create(familyMember.Id, Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(1));

        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(patientUserId);
        _currentUser.UserRole.Returns("Patient");

        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(appt);
        _patientRepo.GetByUserIdAsync(patientUserId, Arg.Any<CancellationToken>()).Returns(patient);
        _patientRepo.GetFamilyMembersAsync(patient.Id, Arg.Any<CancellationToken>()).Returns(new List<Patient> { familyMember });

        await _handler.CancelAsync(id, "Đổi lịch");

        appt.Status.Should().Be(AppointmentStatus.Cancelled);
        await _repo.Received(1).UpdateAsync(appt, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Bệnh nhân hủy lịch hẹn của người khác (không phải của mình hay người thân) sẽ bị từ chối.
    /// </summary>
    [Test]
    public async Task CancelAsync_PatientCancelsOtherAppointment_ThrowsUnauthorizedAccessException()
    {
        var id = Guid.NewGuid();
        var patientUserId = Guid.NewGuid();
        var patient = Patient.Create("Primary Patient", new DateOnly(1990, 1, 1), "Nam", patientUserId);
        var otherPatient = Patient.Create("Other Patient", new DateOnly(1995, 2, 2), "Nữ");
        var appt = Appointment.Create(otherPatient.Id, Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(1));

        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(patientUserId);
        _currentUser.UserRole.Returns("Patient");

        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(appt);
        _patientRepo.GetByUserIdAsync(patientUserId, Arg.Any<CancellationToken>()).Returns(patient);
        _patientRepo.GetFamilyMembersAsync(patient.Id, Arg.Any<CancellationToken>()).Returns(new List<Patient>());

        Func<Task> act = () => _handler.CancelAsync(id, "Hủy lịch người khác");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        appt.Status.Should().NotBe(AppointmentStatus.Cancelled);
        await _repo.DidNotReceive().UpdateAsync(appt, Arg.Any<CancellationToken>());
    }

    // ── CheckInAsync ──────────────────────────────────────────────────────────

    /// <summary>
    /// Khi bệnh nhân check-in, nha sĩ phải nhận thông báo ưu tiên High để chuẩn bị kịp thời —
    /// nha sĩ cần biết ngay bệnh nhân đã đến phòng chờ.
    /// </summary>
    [Test]
    public async Task CheckInAsync_ExistingAppointment_SendsHighPriorityNotificationToDentist()
    {
        var id = Guid.NewGuid();
        // CheckIn chỉ hợp lệ khi lịch hẹn đã ở trạng thái Confirmed
        var appt = MakeAppointment();
        appt.Confirm();
        var dentistUserId = Guid.NewGuid();
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(appt);
        _repo.GetDentistUserIdAsync(appt.DentistId, Arg.Any<CancellationToken>()).Returns(dentistUserId);

        await _handler.CheckInAsync(id);

        await _notification.Received(1).CreateAsync(
            Arg.Is<CreateNotificationRequest>(r => r.Priority == NotificationPriority.High),
            Arg.Any<CancellationToken>());
    }
}
