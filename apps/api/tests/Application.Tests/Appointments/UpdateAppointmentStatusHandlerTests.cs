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

    /// <summary>
    /// appointmentId không tồn tại khi check-in phải ném KeyNotFoundException, không được để lộ
    /// NullReferenceException ra ngoài.
    /// </summary>
    [Test]
    public async Task CheckInAsync_NonExistentAppointment_ThrowsKeyNotFoundException()
    {
        var id = Guid.NewGuid();
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Appointment?)null);

        Func<Task> act = () => _handler.CheckInAsync(id);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    /// <summary>
    /// Check-in chỉ hợp lệ khi lịch hẹn đã ở trạng thái Confirmed; lịch hẹn còn Pending phải bị từ
    /// chối bằng InvalidOperationException — tránh cho khách chưa xác nhận vào thẳng hàng đợi khám.
    /// </summary>
    [Test]
    public async Task CheckInAsync_AppointmentNotConfirmed_ThrowsInvalidOperationException()
    {
        var id = Guid.NewGuid();
        var appt = MakeAppointment(); // Pending
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(appt);

        Func<Task> act = () => _handler.CheckInAsync(id);

        await act.Should().ThrowAsync<InvalidOperationException>();
        await _repo.DidNotReceive().UpdateAsync(Arg.Any<Appointment>(), Arg.Any<CancellationToken>());
    }

    // ── MarkNoShowAsync ───────────────────────────────────────────────────────

    /// <summary>appointmentId không tồn tại phải ném KeyNotFoundException.</summary>
    [Test]
    public async Task MarkNoShowAsync_NonExistentAppointment_ThrowsKeyNotFoundException()
    {
        var id = Guid.NewGuid();
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Appointment?)null);

        Func<Task> act = () => _handler.MarkNoShowAsync(id);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    /// <summary>
    /// Chỉ được ghi nhận vắng mặt với lịch đã Confirmed; lịch còn Pending phải bị từ chối bằng
    /// InvalidOperationException — đúng bối cảnh lễ tân dùng ở quầy check-in.
    /// </summary>
    [Test]
    public async Task MarkNoShowAsync_AppointmentNotConfirmed_ThrowsInvalidOperationException()
    {
        var id = Guid.NewGuid();
        var appt = MakeAppointment(); // Pending
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(appt);

        Func<Task> act = () => _handler.MarkNoShowAsync(id);

        await act.Should().ThrowAsync<InvalidOperationException>();
        await _repo.DidNotReceive().UpdateAsync(Arg.Any<Appointment>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Với lịch hẹn đã Confirmed, MarkNoShowAsync phải chuyển trạng thái sang NoShow và persist.</summary>
    [Test]
    public async Task MarkNoShowAsync_ConfirmedAppointment_SetsStatusToNoShowAndUpdates()
    {
        var id = Guid.NewGuid();
        var appt = MakeAppointment();
        appt.Confirm();
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(appt);

        await _handler.MarkNoShowAsync(id);

        appt.Status.Should().Be(AppointmentStatus.NoShow);
        await _repo.Received(1).UpdateAsync(appt, Arg.Any<CancellationToken>());
    }

    /// <summary>Ghi nhận vắng mặt phải báo cho nha sĩ phụ trách để nha sĩ biết bệnh nhân không đến.</summary>
    [Test]
    public async Task MarkNoShowAsync_ConfirmedAppointment_NotifiesDentist()
    {
        var id = Guid.NewGuid();
        var appt = MakeAppointment();
        appt.Confirm();
        var dentistUserId = Guid.NewGuid();
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(appt);
        _repo.GetDentistUserIdAsync(appt.DentistId, Arg.Any<CancellationToken>()).Returns(dentistUserId);

        await _handler.MarkNoShowAsync(id);

        await _notification.Received(1).CreateAsync(
            Arg.Is<CreateNotificationRequest>(r => r.UserId == dentistUserId),
            Arg.Any<CancellationToken>());
    }

    // ── StartTreatmentAsync ───────────────────────────────────────────────────

    /// <summary>appointmentId không tồn tại phải ném KeyNotFoundException.</summary>
    [Test]
    public async Task StartTreatmentAsync_NonExistentAppointment_ThrowsKeyNotFoundException()
    {
        var id = Guid.NewGuid();
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Appointment?)null);

        Func<Task> act = () => _handler.StartTreatmentAsync(id);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    /// <summary>Chỉ được bắt đầu khám khi lịch hẹn đã CheckedIn; lịch Pending phải bị từ chối.</summary>
    [Test]
    public async Task StartTreatmentAsync_AppointmentNotCheckedIn_ThrowsInvalidOperationException()
    {
        var id = Guid.NewGuid();
        var appt = MakeAppointment(); // Pending
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(appt);

        Func<Task> act = () => _handler.StartTreatmentAsync(id);

        await act.Should().ThrowAsync<InvalidOperationException>();
        await _repo.DidNotReceive().UpdateAsync(Arg.Any<Appointment>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Với lịch hẹn đã CheckedIn, StartTreatmentAsync phải chuyển sang InProgress và persist.</summary>
    [Test]
    public async Task StartTreatmentAsync_CheckedInAppointment_SetsStatusToInProgress()
    {
        var id = Guid.NewGuid();
        var appt = MakeAppointment();
        appt.Confirm();
        appt.CheckIn();
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(appt);

        await _handler.StartTreatmentAsync(id);

        appt.Status.Should().Be(AppointmentStatus.InProgress);
        await _repo.Received(1).UpdateAsync(appt, Arg.Any<CancellationToken>());
    }

    // ── CompleteAsync ─────────────────────────────────────────────────────────

    /// <summary>appointmentId không tồn tại phải ném KeyNotFoundException.</summary>
    [Test]
    public async Task CompleteAsync_NonExistentAppointment_ThrowsKeyNotFoundException()
    {
        var id = Guid.NewGuid();
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Appointment?)null);

        Func<Task> act = () => _handler.CompleteAsync(id);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    /// <summary>Hoàn thành lịch hẹn phải chuyển trạng thái sang Completed, persist và ghi activity log.</summary>
    [Test]
    public async Task CompleteAsync_ExistingAppointment_SetsStatusToCompletedAndLogsActivity()
    {
        var id = Guid.NewGuid();
        var appt = MakeAppointment();
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(appt);

        await _handler.CompleteAsync(id);

        appt.Status.Should().Be(AppointmentStatus.Completed);
        await _repo.Received(1).UpdateAsync(appt, Arg.Any<CancellationToken>());
        await _activityLog.Received(1).LogAsync(
            userId: Arg.Any<Guid?>(), userName: Arg.Any<string>(), userRole: Arg.Any<string>(),
            action: Arg.Any<string>(), module: Arg.Any<string>(), description: Arg.Any<string>(),
            status: Arg.Any<string>(), ipAddress: Arg.Any<string?>(), targetId: Arg.Any<string?>(),
            ct: Arg.Any<CancellationToken>());
    }

    // ── EndTreatmentAsync ─────────────────────────────────────────────────────

    /// <summary>appointmentId không tồn tại phải ném KeyNotFoundException.</summary>
    [Test]
    public async Task EndTreatmentAsync_NonExistentAppointment_ThrowsKeyNotFoundException()
    {
        var id = Guid.NewGuid();
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Appointment?)null);

        Func<Task> act = () => _handler.EndTreatmentAsync(id);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    /// <summary>Chỉ được kết thúc điều trị khi đang InProgress; lịch Pending phải bị từ chối.</summary>
    [Test]
    public async Task EndTreatmentAsync_AppointmentNotInProgress_ThrowsInvalidOperationException()
    {
        var id = Guid.NewGuid();
        var appt = MakeAppointment(); // Pending
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(appt);

        Func<Task> act = () => _handler.EndTreatmentAsync(id);

        await act.Should().ThrowAsync<InvalidOperationException>();
        await _repo.DidNotReceive().UpdateAsync(Arg.Any<Appointment>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Kết thúc điều trị hợp lệ phải chuyển trạng thái sang PendingPayment (chờ thanh toán) và persist.</summary>
    [Test]
    public async Task EndTreatmentAsync_InProgressAppointment_SetsStatusToPendingPayment()
    {
        var id = Guid.NewGuid();
        var appt = MakeAppointment();
        appt.Confirm();
        appt.CheckIn();
        appt.StartTreatment();
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(appt);

        await _handler.EndTreatmentAsync(id);

        appt.Status.Should().Be(AppointmentStatus.PendingPayment);
        await _repo.Received(1).UpdateAsync(appt, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Khi bác sĩ đặt lịch tái khám (FollowUpDate) và bệnh nhân có tài khoản liên kết, phải gửi
    /// thông báo nhắc tái khám cho bệnh nhân — nếu không, bệnh nhân sẽ không biết để quay lại khám.
    /// </summary>
    [Test]
    public async Task EndTreatmentAsync_WithFollowUpDateAndLinkedPatient_SendsReminderNotification()
    {
        var id = Guid.NewGuid();
        var patientUserId = Guid.NewGuid();
        var patient = Patient.Create("Bệnh nhân", new DateOnly(1990, 1, 1), "Nam", patientUserId);
        var appt = Appointment.Create(patient.Id, Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(-1));
        appt.Confirm();
        appt.CheckIn();
        appt.StartTreatment();
        appt.SetFollowUpReminder(DateOnly.FromDateTime(DateTime.Today.AddMonths(1)), "Tái khám định kỳ");
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(appt);
        _patientRepo.GetByIdAsync(patient.Id, Arg.Any<CancellationToken>()).Returns(patient);

        await _handler.EndTreatmentAsync(id);

        await _notification.Received(1).CreateAsync(
            Arg.Is<CreateNotificationRequest>(r => r.UserId == patientUserId && r.Type == NotificationType.Reminder),
            Arg.Any<CancellationToken>());
    }

    /// <summary>Không đặt lịch tái khám (FollowUpDate null) thì không được gửi thông báo nhắc tái khám.</summary>
    [Test]
    public async Task EndTreatmentAsync_WithoutFollowUpDate_DoesNotSendReminderNotification()
    {
        var id = Guid.NewGuid();
        var appt = MakeAppointment();
        appt.Confirm();
        appt.CheckIn();
        appt.StartTreatment();
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(appt);

        await _handler.EndTreatmentAsync(id);

        await _notification.DidNotReceive().CreateAsync(
            Arg.Is<CreateNotificationRequest>(r => r.Type == NotificationType.Reminder),
            Arg.Any<CancellationToken>());
    }

    // ── CancelAsync: các nhánh quyền hạn khác ─────────────────────────────────

    /// <summary>
    /// Vai trò Patient nhưng handler không được cấu hình IPatientRepository (null) phải ném
    /// InvalidOperationException — tránh NullReferenceException khi thiếu cấu hình DI.
    /// </summary>
    [Test]
    public async Task CancelAsync_PatientRoleWithoutPatientRepositoryConfigured_ThrowsInvalidOperationException()
    {
        var handlerWithoutPatientRepo = new UpdateAppointmentStatusHandler(
            _repo, _activityLog, _notification, _currentUser, patientRepository: null);
        var id = Guid.NewGuid();
        var appt = MakeAppointment();
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(appt);
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserRole.Returns("Patient");

        Func<Task> act = () => handlerWithoutPatientRepo.CancelAsync(id);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    /// <summary>
    /// Vai trò Patient nhưng currentUser.UserId là null (token thiếu claim) phải ném
    /// UnauthorizedAccessException thay vì NullReferenceException.
    /// </summary>
    [Test]
    public async Task CancelAsync_PatientRoleWithNullUserId_ThrowsUnauthorizedAccessException()
    {
        var id = Guid.NewGuid();
        var appt = MakeAppointment();
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(appt);
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserRole.Returns("Patient");
        _currentUser.UserId.Returns((Guid?)null);

        Func<Task> act = () => _handler.CancelAsync(id);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    /// <summary>
    /// Vai trò Patient nhưng không tìm thấy hồ sơ Patient tương ứng với tài khoản phải ném
    /// UnauthorizedAccessException — tránh cho phép hủy khi không xác định được chủ sở hữu.
    /// </summary>
    [Test]
    public async Task CancelAsync_PatientRoleWithNoMatchingPatientRecord_ThrowsUnauthorizedAccessException()
    {
        var id = Guid.NewGuid();
        var patientUserId = Guid.NewGuid();
        var appt = MakeAppointment();
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(appt);
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserRole.Returns("Patient");
        _currentUser.UserId.Returns(patientUserId);
        _patientRepo.GetByUserIdAsync(patientUserId, Arg.Any<CancellationToken>()).Returns((Patient?)null);

        Func<Task> act = () => _handler.CancelAsync(id);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }
}
