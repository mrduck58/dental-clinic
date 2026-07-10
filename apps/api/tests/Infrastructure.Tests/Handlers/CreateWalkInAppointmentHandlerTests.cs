using DentalClinic.API.Application.UseCases.Appointments;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Handlers;

[TestFixture]
public class CreateWalkInAppointmentHandlerTests
{
    private AppDbContext _db = null!;
    private CreateWalkInAppointmentHandler _handler = null!;
    private Guid _dentistId;

    [SetUp]
    public async Task SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _handler = new CreateWalkInAppointmentHandler(_db);

        var dentistUser = User.Create("d1", $"d1-{Guid.NewGuid()}@test.com", "hash", "Dentist");
        _db.Users.Add(dentistUser);
        var dentist = Dentist.Create(dentistUser.Id, "BS. Nguyễn Văn Hùng", "Nha khoa tổng quát", 5);
        _db.Dentists.Add(dentist);
        await _db.SaveChangesAsync();
        _dentistId = dentist.Id;
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

        Func<Task> act = async () => await _handler.HandleAsync(MakeCommand(pastDate));

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

        var result = await _handler.HandleAsync(MakeCommand(futureDate));

        result.Status.Should().Be("CheckedIn");
        result.PatientName.Should().Be("Nguyễn Văn A");
    }

    [Test]
    public async Task HandleAsync_SlotAlreadyBooked_ThrowsConflictException()
    {
        var futureDate = DateTimeOffset.UtcNow.AddHours(1);
        await _handler.HandleAsync(MakeCommand(futureDate));

        Func<Task> act = async () => await _handler.HandleAsync(MakeCommand(futureDate));

        await act.Should().ThrowAsync<ConflictException>();
    }

    /// <summary>
    /// Khi staff chọn hồ sơ từ ô tra cứu, lịch hẹn phải gắn vào đúng hồ sơ đó — kể cả khi
    /// số điện thoại nhập ở form khác với số đang lưu (bệnh nhân đổi số).
    /// </summary>
    [Test]
    public async Task HandleAsync_WithPatientId_ReusesThatPatientAndUpdatesPhone()
    {
        var existing = Patient.Create("Trần Thị B", new DateOnly(1985, 5, 20), "Nữ", phoneNumber: "0900000001");
        _db.Patients.Add(existing);
        await _db.SaveChangesAsync();

        var cmd = MakeCommand(DateTimeOffset.UtcNow.AddHours(1)) with { PatientId = existing.Id, PatientPhone = "0988887777" };
        var result = await _handler.HandleAsync(cmd);

        _db.Patients.Should().HaveCount(1);
        var appointment = await _db.Appointments.SingleAsync();
        appointment.PatientId.Should().Be(existing.Id);
        result.PatientName.Should().Be("Trần Thị B");
        (await _db.Patients.SingleAsync()).PhoneNumber.Should().Be("0988887777");
    }

    /// <summary>Không tìm thấy hồ sơ đã chọn (đã bị xoá) thì báo lỗi thay vì âm thầm tạo mới.</summary>
    [Test]
    public async Task HandleAsync_WithUnknownPatientId_ThrowsValidationException()
    {
        var cmd = MakeCommand(DateTimeOffset.UtcNow.AddHours(1)) with { PatientId = Guid.NewGuid() };

        Func<Task> act = async () => await _handler.HandleAsync(cmd);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>
    /// Bệnh nhân tạo tại quầy không có tài khoản, số điện thoại chỉ nằm ở Patient.PhoneNumber.
    /// Lần tái khám sau phải khớp lại đúng hồ sơ cũ chứ không sinh bản ghi trùng.
    /// </summary>
    [Test]
    public async Task HandleAsync_SamePhoneAsAccountlessPatient_ReusesExistingPatient()
    {
        await _handler.HandleAsync(MakeCommand(DateTimeOffset.UtcNow.AddHours(1)));

        await _handler.HandleAsync(MakeCommand(DateTimeOffset.UtcNow.AddHours(2)));

        _db.Patients.Should().HaveCount(1);
        _db.Appointments.Should().HaveCount(2);
    }
}
