using DentalClinic.API.Application.UseCases.Booking;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Infrastructure.Persistence;
using DentalClinic.API.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Handlers;

[TestFixture]
public class CreateWalkInAppointmentHandlerTests
{
    private AppDbContext _db = null!;
    private INotificationService _notificationService = null!;
    private CreateWalkInAppointmentHandler _handler = null!;
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
        _handler = new CreateWalkInAppointmentHandler(
            new AppointmentRepository(_db), new PatientRepository(_db), new UserRepository(_db), _notificationService);

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

    /// <summary>Không cho đặt lịch cho khung giờ đã qua — chặn cả trường hợp bypass UI.</summary>
    [Test]
    public async Task HandleAsync_PastAppointmentDate_ThrowsValidationException()
    {
        var pastDate = DateTimeOffset.UtcNow.AddHours(-1);

        Func<Task> act = async () => await _handler.Handle(MakeCommand(pastDate), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
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
        cancelledAppointment.Cancel("Đổi ý");
        _db.Appointments.Add(cancelledAppointment);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(MakeCommand(futureDate), CancellationToken.None);

        result.Status.Should().Be("CheckedIn");
        _db.Appointments.Should().HaveCount(2);
    }
}
