using DentalClinic.API.Application.UseCases.Booking;
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
public class RescheduleAppointmentHandlerTests
{
    private IAppointmentRepository _repo = null!;
    private IPatientRepository _patientRepo = null!;
    private IServiceRepository _serviceRepo = null!;
    private ICurrentUserService _currentUser = null!;
    private IActivityLogService _activityLog = null!;
    private INotificationService _notification = null!;
    private RescheduleAppointmentHandler _handler = null!;

    private static readonly Guid PatientUserId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _repo = Substitute.For<IAppointmentRepository>();
        _patientRepo = Substitute.For<IPatientRepository>();
        _serviceRepo = Substitute.For<IServiceRepository>();
        _currentUser = Substitute.For<ICurrentUserService>();
        _activityLog = Substitute.For<IActivityLogService>();
        _notification = Substitute.For<INotificationService>();

        _repo.GetByDateAsync(Arg.Any<DateOnly>(), Arg.Any<CancellationToken>()).Returns(new List<Appointment>());
        _repo.GetDentistUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Guid?)null);

        _handler = new RescheduleAppointmentHandler(
            _repo, _patientRepo,
            new AppointmentChangeGuard(_currentUser, _patientRepo, _repo),
            new AppointmentSlotGuard(_repo, _serviceRepo),
            _activityLog, _notification, _currentUser);
    }

    /// <summary>Nhân viên phòng khám — không bị áp hạn 24 giờ, số lần dời, hay yêu cầu xác nhận lại.</summary>
    private void ActAsStaff()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserRole.Returns("Staff");
        _currentUser.UserId.Returns(Guid.NewGuid());
    }

    /// <summary>Bệnh nhân sở hữu <paramref name="appointment"/>.</summary>
    private Patient ActAsOwningPatient(Appointment appointment)
    {
        var patient = Patient.Create(PatientUserId, new DateOnly(1990, 1, 1), "Nam");
        typeof(Appointment).GetProperty("PatientId")!.SetValue(appointment, patient.Id);

        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserRole.Returns("Patient");
        _currentUser.UserId.Returns(PatientUserId);
        _patientRepo.GetByUserIdAsync(PatientUserId, Arg.Any<CancellationToken>()).Returns(patient);
        _patientRepo.GetFamilyMembersAsync(patient.Id, Arg.Any<CancellationToken>()).Returns(new List<Patient>());
        return patient;
    }

    private Appointment SeedAppointment(int daysAhead = 5)
    {
        var appointment = Appointment.Create(
            Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(daysAhead));
        _repo.GetByIdAsync(appointment.Id, Arg.Any<CancellationToken>()).Returns(appointment);
        return appointment;
    }

    private Task<RescheduleAppointmentResult> RescheduleTo(
        Appointment appointment, DateTimeOffset newDate, Guid? dentistId = null) =>
        _handler.Handle(
            new RescheduleAppointmentCommand(appointment.Id, newDate, dentistId, null, Reason: null),
            CancellationToken.None);

    // ── Luồng thành công ──────────────────────────────────────────────────────

    /// <summary>Dời lịch phải SỬA bản ghi hiện có, tuyệt đối không tạo bản ghi mới.</summary>
    [Test]
    public async Task Handle_ValidReschedule_UpdatesInPlaceWithoutCreatingNewAppointment()
    {
        ActAsStaff();
        var appointment = SeedAppointment();
        var newDate = DateTimeOffset.UtcNow.AddDays(7);

        var result = await RescheduleTo(appointment, newDate);

        result.AppointmentId.Should().Be(appointment.Id, "dời lịch không được đổi Id");
        appointment.AppointmentDate.Should().Be(newDate);
        appointment.RescheduledCount.Should().Be(1);
        await _repo.Received(1).UpdateAsync(appointment, Arg.Any<CancellationToken>());
        await _repo.DidNotReceive().AddAsync(Arg.Any<Appointment>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Bệnh nhân tự dời ⇒ lịch quay về Pending để phòng khám sắp xếp lại nhân sự.</summary>
    [Test]
    public async Task Handle_PatientReschedules_ResetsStatusToPending()
    {
        var appointment = SeedAppointment();
        appointment.Confirm();
        ActAsOwningPatient(appointment);

        await RescheduleTo(appointment, DateTimeOffset.UtcNow.AddDays(7));

        appointment.Status.Should().Be(AppointmentStatus.Pending);
    }

    /// <summary>Nhân viên dời ⇒ giữ nguyên trạng thái, vì chính họ đang là người sắp xếp.</summary>
    [Test]
    public async Task Handle_StaffReschedules_KeepsConfirmedStatus()
    {
        ActAsStaff();
        var appointment = SeedAppointment();
        appointment.Confirm();

        await RescheduleTo(appointment, DateTimeOffset.UtcNow.AddDays(7));

        appointment.Status.Should().Be(AppointmentStatus.Confirmed);
    }

    /// <summary>Bỏ trống DentistId nghĩa là giữ nguyên bác sĩ, không phải xóa bác sĩ.</summary>
    [Test]
    public async Task Handle_NullDentistId_KeepsCurrentDentist()
    {
        ActAsStaff();
        var appointment = SeedAppointment();
        var originalDentist = appointment.DentistId;

        await RescheduleTo(appointment, DateTimeOffset.UtcNow.AddDays(7), dentistId: null);

        appointment.DentistId.Should().Be(originalDentist);
    }

    // ── Giới hạn dành riêng cho bệnh nhân ─────────────────────────────────────

    /// <summary>Cùng tình huống nhưng người thao tác là nhân viên — không bị chặn, vì họ đang xử lý cuộc gọi phút chót.</summary>
    [Test]
    public async Task Handle_StaffWithinDeadline_IsAllowed()
    {
        ActAsStaff();
        var appointment = SeedAppointment(daysAhead: 5);
        typeof(Appointment).GetProperty("CreatedAt")!
            .SetValue(appointment, DateTimeOffset.UtcNow.AddHours(-25));

        Func<Task> act = () => RescheduleTo(appointment, DateTimeOffset.UtcNow.AddDays(7));

        await act.Should().NotThrowAsync();
    }

    // ── Quyền sở hữu và trạng thái ────────────────────────────────────────────

    /// <summary>Lịch của người khác trả 404 y như không tồn tại — không xác nhận id có thật.</summary>
    [Test]
    public async Task Handle_PatientReschedulesSomeoneElsesAppointment_ThrowsNotFound()
    {
        var appointment = SeedAppointment();
        var patient = Patient.Create(PatientUserId, new DateOnly(1990, 1, 1), "Nam");

        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserRole.Returns("Patient");
        _currentUser.UserId.Returns(PatientUserId);
        _patientRepo.GetByUserIdAsync(PatientUserId, Arg.Any<CancellationToken>()).Returns(patient);
        _patientRepo.GetFamilyMembersAsync(patient.Id, Arg.Any<CancellationToken>()).Returns(new List<Patient>());

        Func<Task> act = () => RescheduleTo(appointment, DateTimeOffset.UtcNow.AddDays(7));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>Chủ hộ dời hộ lịch của người nhà là luồng hợp lệ.</summary>
    [Test]
    public async Task Handle_PatientReschedulesFamilyMemberAppointment_Succeeds()
    {
        var appointment = SeedAppointment();
        var head = Patient.Create(PatientUserId, new DateOnly(1990, 1, 1), "Nam");
        var member = Patient.Create(Guid.Empty, new DateOnly(2015, 5, 5), "Nam",
            primaryPatientId: head.Id, relationship: "Con trai");
        typeof(Appointment).GetProperty("PatientId")!.SetValue(appointment, member.Id);

        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserRole.Returns("Patient");
        _currentUser.UserId.Returns(PatientUserId);
        _patientRepo.GetByUserIdAsync(PatientUserId, Arg.Any<CancellationToken>()).Returns(head);
        _patientRepo.GetFamilyMembersAsync(head.Id, Arg.Any<CancellationToken>()).Returns(new List<Patient> { member });

        Func<Task> act = () => RescheduleTo(appointment, DateTimeOffset.UtcNow.AddDays(7));

        await act.Should().NotThrowAsync();
    }

    /// <summary>Đã check-in trở đi thì buổi khám đã diễn ra trên thực tế — không dời được nữa.</summary>
    [TestCase(AppointmentStatus.CheckedIn)]
    [TestCase(AppointmentStatus.InProgress)]
    [TestCase(AppointmentStatus.Completed)]
    [TestCase(AppointmentStatus.Cancelled)]
    public async Task Handle_NonChangeableStatus_ThrowsConflict(AppointmentStatus status)
    {
        ActAsStaff();
        var appointment = SeedAppointment();
        typeof(Appointment).GetProperty("Status")!.SetValue(appointment, status);

        Func<Task> act = () => RescheduleTo(appointment, DateTimeOffset.UtcNow.AddDays(7));

        await act.Should().ThrowAsync<ConflictException>();
        await _repo.DidNotReceive().UpdateAsync(Arg.Any<Appointment>(), Arg.Any<CancellationToken>());
    }

    // ── Khung giờ ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Chính lịch hẹn đang dời phải được loại khỏi danh sách chiếm chỗ — nếu không, đổi bác sĩ mà giữ
    /// nguyên giờ sẽ bị chính nó chặn.
    /// </summary>
    [Test]
    public async Task Handle_SameTimeDifferentDentist_DoesNotBlockItself()
    {
        ActAsStaff();
        var appointment = SeedAppointment();
        _repo.GetByDateAsync(Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new List<Appointment> { appointment });

        Func<Task> act = () => RescheduleTo(appointment, appointment.AppointmentDate, dentistId: Guid.NewGuid());

        await act.Should().NotThrowAsync();
    }

    /// <summary>Khung giờ đã có lịch của bác sĩ khác chiếm thì phải báo trùng.</summary>
    [Test]
    public async Task Handle_SlotTakenByAnotherAppointment_ThrowsConflict()
    {
        ActAsStaff();
        var appointment = SeedAppointment();
        var targetDate = DateTimeOffset.UtcNow.AddDays(7);

        var blocking = Appointment.Create(Guid.NewGuid(), Guid.NewGuid(), targetDate);
        _repo.GetByDateAsync(Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new List<Appointment> { blocking });

        Func<Task> act = () => RescheduleTo(appointment, targetDate, dentistId: blocking.DentistId);

        await act.Should().ThrowAsync<ConflictException>();
    }

    /// <summary>Không cho dời về quá khứ.</summary>
    [Test]
    public async Task Handle_PastDate_ThrowsValidation()
    {
        ActAsStaff();
        var appointment = SeedAppointment();

        Func<Task> act = () => RescheduleTo(appointment, DateTimeOffset.UtcNow.AddDays(-1));

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Lịch hẹn không tồn tại phải trả 404, không phải 500.</summary>
    [Test]
    public async Task Handle_UnknownAppointment_ThrowsNotFound()
    {
        ActAsStaff();
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Appointment?)null);

        Func<Task> act = () => _handler.Handle(
            new RescheduleAppointmentCommand(Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(7), null, null, null),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>Không cho dời lịch sang ngày mà bệnh nhân đã có một lịch hẹn khác đang hoạt động.</summary>
    [Test]
    public async Task Handle_TargetDateAlreadyHasActiveAppointment_ThrowsConflict()
    {
        ActAsStaff();
        var appointment = SeedAppointment();
        _repo.HasActiveAppointmentOnDateAsync(
            Arg.Any<Guid>(), Arg.Any<DateOnly>(), appointment.Id, Arg.Any<CancellationToken>())
            .Returns(true);

        Func<Task> act = () => RescheduleTo(appointment, DateTimeOffset.UtcNow.AddDays(3));

        await act.Should().ThrowAsync<ConflictException>();
    }
}
