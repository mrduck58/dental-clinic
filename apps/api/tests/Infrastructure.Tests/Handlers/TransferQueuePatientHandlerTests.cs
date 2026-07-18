using DentalClinic.API.Application.UseCases.Appointments;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Handlers;

[TestFixture]
public class TransferQueuePatientHandlerTests
{
    private static readonly TimeZoneInfo VietnamTz = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

    private AppDbContext _db = null!;
    private IActivityLogService _activityLogService = null!;
    private INotificationService _notificationService = null!;
    private ICurrentUserService _currentUser = null!;
    private TransferQueuePatientHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _activityLogService = Substitute.For<IActivityLogService>();
        _notificationService = Substitute.For<INotificationService>();
        _currentUser = Substitute.For<ICurrentUserService>();
        _currentUser.UserId.Returns(Guid.NewGuid());
        _currentUser.UserName.Returns("Lễ tân Test");
        _currentUser.UserRole.Returns("Staff");
        _handler = new TransferQueuePatientHandler(_db, _activityLogService, _notificationService, _currentUser);
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    private static DateTimeOffset NowVietnam() => TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, VietnamTz);
    private static string CurrentShiftCode() => NowVietnam().Hour < 12 ? "morning" : "afternoon";

    /// <summary>Thiếu tên phòng đích phải báo lỗi ValidationException.</summary>
    [Test]
    public async Task HandleAsync_EmptyRoomName_ThrowsValidationException()
    {
        Func<Task> act = () => _handler.HandleAsync(Guid.NewGuid(), "  ");

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Lịch hẹn không tồn tại phải báo lỗi NotFoundException.</summary>
    [Test]
    public async Task HandleAsync_AppointmentNotFound_ThrowsNotFoundException()
    {
        Func<Task> act = () => _handler.HandleAsync(Guid.NewGuid(), "Phòng 101");

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>Chỉ chuyển được bệnh nhân đang ở trạng thái chờ (CheckedIn) — buổi hẹn Pending
    /// hoặc đang khám không được phép chuyển phòng.</summary>
    [Test]
    public async Task HandleAsync_AppointmentNotCheckedIn_ThrowsConflictException()
    {
        var dentistUser = User.Create("tq1", $"tq1-{Guid.NewGuid()}@test.com", "hash", "Dentist");
        _db.Users.Add(dentistUser);
        var dentist = Dentist.Create(dentistUser.Id, "Nha khoa tổng quát", 5);
        var patient = Patient.Create(Guid.Empty, new DateOnly(1990, 1, 1), "Nam");
        _db.Dentists.Add(dentist);
        _db.Patients.Add(patient);
        var appointment = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        appointment.Confirm();
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();

        Func<Task> act = () => _handler.HandleAsync(appointment.Id, "Phòng 101");

        await act.Should().ThrowAsync<ConflictException>();
    }

    /// <summary>Không có bác sĩ nào đang trong ca trực tại phòng đích phải báo lỗi ConflictException.</summary>
    [Test]
    public async Task HandleAsync_NoDentistOnShiftAtTargetRoom_ThrowsConflictException()
    {
        var dentistUser = User.Create("tq2", $"tq2-{Guid.NewGuid()}@test.com", "hash", "Dentist");
        _db.Users.Add(dentistUser);
        var dentist = Dentist.Create(dentistUser.Id, "Nha khoa tổng quát", 5);
        var patient = Patient.Create(Guid.Empty, new DateOnly(1990, 1, 1), "Nam");
        _db.Dentists.Add(dentist);
        _db.Patients.Add(patient);
        var appointment = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        appointment.CheckIn();
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();

        Func<Task> act = () => _handler.HandleAsync(appointment.Id, "Phòng Không Có Ai");

        await act.Should().ThrowAsync<ConflictException>();
    }

    /// <summary>Chuyển bệnh nhân sang bác sĩ đang trực đúng phòng đích phải cập nhật bác sĩ phụ trách,
    /// ghi log hoạt động và gửi thông báo cho bác sĩ nhận.</summary>
    [Test]
    public async Task HandleAsync_ValidTransfer_ReassignsDentistLogsAndNotifies()
    {
        var today = DateOnly.FromDateTime(NowVietnam().Date);
        var shift = CurrentShiftCode();

        var sourceDentistUser = User.Create("tq3", $"tq3-{Guid.NewGuid()}@test.com", "hash", "Dentist", fullName: "BS Nguồn");
        var targetDentistUser = User.Create("tq4", $"tq4-{Guid.NewGuid()}@test.com", "hash", "Dentist", fullName: "BS Đích");
        _db.Users.AddRange(sourceDentistUser, targetDentistUser);
        var sourceDentist = Dentist.Create(sourceDentistUser.Id, "Nha khoa tổng quát", 5);
        var targetDentist = Dentist.Create(targetDentistUser.Id, "Nha khoa tổng quát", 5);
        sourceDentist.User = sourceDentistUser;
        targetDentist.User = targetDentistUser;
        var patient = Patient.Create(Guid.Empty, new DateOnly(1990, 1, 1), "Nam");
        _db.Dentists.AddRange(sourceDentist, targetDentist);
        _db.Patients.Add(patient);
        _db.WorkSchedules.Add(WorkSchedule.Create(
            today, shift, "dentist", "dentist", targetDentist.FullName, "Phòng Đích", "border-primary", false));
        var appointment = Appointment.Create(patient.Id, sourceDentist.Id, DateTimeOffset.UtcNow);
        appointment.CheckIn();
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();

        var result = await _handler.HandleAsync(appointment.Id, "Phòng Đích");

        result.DentistId.Should().Be(targetDentist.Id);
        (await _db.Appointments.SingleAsync(a => a.Id == appointment.Id)).DentistId.Should().Be(targetDentist.Id);
        await _activityLogService.Received(1).LogAsync(
            Arg.Any<Guid?>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
        await _notificationService.Received(1).CreateAsync(
            Arg.Is<CreateNotificationRequest>(r => r.UserId == targetDentistUser.Id),
            Arg.Any<CancellationToken>());
    }

    /// <summary>Chuyển "sang" đúng bác sĩ đang phụ trách hiện tại phải là no-op — không ghi log,
    /// không gửi thông báo thừa.</summary>
    [Test]
    public async Task HandleAsync_TargetIsSameAsCurrentDentist_IsNoOp()
    {
        var today = DateOnly.FromDateTime(NowVietnam().Date);
        var shift = CurrentShiftCode();
        var dentistUser = User.Create("tq5", $"tq5-{Guid.NewGuid()}@test.com", "hash", "Dentist", fullName: "BS Trực");
        _db.Users.Add(dentistUser);
        var dentist = Dentist.Create(dentistUser.Id, "Nha khoa tổng quát", 5);
        dentist.User = dentistUser;
        var patient = Patient.Create(Guid.Empty, new DateOnly(1990, 1, 1), "Nam");
        _db.Dentists.Add(dentist);
        _db.Patients.Add(patient);
        _db.WorkSchedules.Add(WorkSchedule.Create(
            today, shift, "dentist", "dentist", dentist.FullName, "Phòng Hiện Tại", "border-primary", false));
        var appointment = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        appointment.CheckIn();
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();

        var result = await _handler.HandleAsync(appointment.Id, "Phòng Hiện Tại");

        result.DentistId.Should().Be(dentist.Id);
        await _activityLogService.DidNotReceive().LogAsync(
            Arg.Any<Guid?>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
        await _notificationService.DidNotReceive().CreateAsync(Arg.Any<CreateNotificationRequest>(), Arg.Any<CancellationToken>());
    }
}
