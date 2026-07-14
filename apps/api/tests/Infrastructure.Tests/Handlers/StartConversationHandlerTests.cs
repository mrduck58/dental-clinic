using DentalClinic.API.Application.UseCases.Chat;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Handlers;

[TestFixture]
public class StartConversationHandlerTests
{
    private AppDbContext _db = null!;
    private IPatientRepository _patientRepo = null!;
    private IUserRepository _userRepo = null!;
    private StartConversationHandler _handler = null!;
    private Guid _userId;
    private Patient _patient = null!;

    [SetUp]
    public async Task SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);

        var user = User.Create($"patient-{Guid.NewGuid()}", $"{Guid.NewGuid()}@test.com", "hash", "Patient", fullName: "Bệnh nhân Test");
        _db.Users.Add(user);
        _userId = user.Id;

        _patient = Patient.Create("Bệnh nhân Test", new DateOnly(1990, 1, 1), "Nam", user.Id);
        _db.Patients.Add(_patient);
        await _db.SaveChangesAsync();

        _patientRepo = Substitute.For<IPatientRepository>();
        _patientRepo.GetByUserIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(_patient);
        _patientRepo.GetFamilyMembersAsync(_patient.Id, Arg.Any<CancellationToken>())
            .Returns(new List<Patient>());

        _userRepo = Substitute.For<IUserRepository>();

        _handler = new StartConversationHandler(_patientRepo, _userRepo, _db);
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    /// <summary>Không có lịch hẹn sắp tới → InitialMessage phải là null, không nhắc gì cả.</summary>
    [Test]
    public async Task HandleAsync_NoUpcomingAppointment_ReturnsNullInitialMessage()
    {
        var result = await _handler.HandleAsync(_userId);

        result.InitialMessage.Should().BeNull();
    }

    /// <summary>Có lịch hẹn Pending trong vòng 48h tới → phải chủ động nhắc ngay khi tạo hội thoại mới,
    /// và tin nhắn nhắc đó phải được lưu lại trong lịch sử hội thoại.</summary>
    [Test]
    public async Task HandleAsync_HasAppointmentWithin48Hours_ReturnsAndPersistsReminderMessage()
    {
        var (dentist, _) = await SeedDentistAsync();
        var appointment = Appointment.Create(_patient.Id, dentist.Id, DateTimeOffset.UtcNow.AddHours(20));
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();

        var result = await _handler.HandleAsync(_userId);

        result.InitialMessage.Should().NotBeNull();
        result.InitialMessage.Should().Contain(dentist.FullName);

        var savedMessage = _db.ChatMessages.Single(m => m.ConversationId == result.ConversationId);
        savedMessage.Role.Should().Be("assistant");
        savedMessage.Content.Should().Be(result.InitialMessage);
    }

    /// <summary>Lịch hẹn còn quá xa (ngoài cửa sổ 48h) không được nhắc — tránh làm phiền không cần thiết.</summary>
    [Test]
    public async Task HandleAsync_AppointmentFarInFuture_DoesNotRemind()
    {
        var (dentist, _) = await SeedDentistAsync();
        var appointment = Appointment.Create(_patient.Id, dentist.Id, DateTimeOffset.UtcNow.AddDays(10));
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();

        var result = await _handler.HandleAsync(_userId);

        result.InitialMessage.Should().BeNull();
    }

    /// <summary>Lịch hẹn đã bị hủy không được nhắc dù còn trong cửa sổ 48h.</summary>
    [Test]
    public async Task HandleAsync_CancelledAppointment_DoesNotRemind()
    {
        var (dentist, _) = await SeedDentistAsync();
        var appointment = Appointment.Create(_patient.Id, dentist.Id, DateTimeOffset.UtcNow.AddHours(10));
        appointment.Cancel("Bệnh nhân bận");
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();

        var result = await _handler.HandleAsync(_userId);

        result.InitialMessage.Should().BeNull();
    }

    private async Task<(Dentist dentist, User user)> SeedDentistAsync()
    {
        var dentistUser = User.Create($"dentist-{Guid.NewGuid()}", $"{Guid.NewGuid()}@test.com", "hash", "Dentist", fullName: "BS Nguyễn Văn A");
        _db.Users.Add(dentistUser);
        var dentist = Dentist.Create(dentistUser.Id, "BS Nguyễn Văn A", "Chỉnh nha", 5);
        _db.Dentists.Add(dentist);
        await _db.SaveChangesAsync();
        return (dentist, dentistUser);
    }
}
