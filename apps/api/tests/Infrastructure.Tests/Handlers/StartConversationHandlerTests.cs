using DentalClinic.API.Application.UseCases.Chat;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Infrastructure.Persistence;
using DentalClinic.API.Infrastructure.Persistence.Repositories;
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

        var user = User.Create($"patient-{Guid.NewGuid()}", $"{Guid.NewGuid()}@test.com", "hash", UserRole.Patient, fullName: "Bệnh nhân Test");
        _db.Users.Add(user);
        _userId = user.Id;

        _patient = Patient.Create(user.Id, new DateOnly(1990, 1, 1), "Nam");
        _db.Patients.Add(_patient);
        await _db.SaveChangesAsync();

        _patientRepo = Substitute.For<IPatientRepository>();
        _patientRepo.GetByUserIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(_patient);
        _patientRepo.GetFamilyMembersAsync(_patient.Id, Arg.Any<CancellationToken>())
            .Returns(new List<Patient>());

        _userRepo = Substitute.For<IUserRepository>();

        _handler = new StartConversationHandler(
            _patientRepo, _userRepo, new ChatConversationRepository(_db), new ChatMessageRepository(_db), _db);
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    /// <summary>Không có lịch hẹn sắp tới → InitialMessage phải là null, không nhắc gì cả.</summary>
    [Test]
    public async Task HandleAsync_NoUpcomingAppointment_ReturnsNullInitialMessage()
    {
        var result = await _handler.Handle(new StartConversationCommand(_userId), CancellationToken.None);

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

        var result = await _handler.Handle(new StartConversationCommand(_userId), CancellationToken.None);

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

        var result = await _handler.Handle(new StartConversationCommand(_userId), CancellationToken.None);

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

        var result = await _handler.Handle(new StartConversationCommand(_userId), CancellationToken.None);

        result.InitialMessage.Should().BeNull();
    }

    private async Task<(DentistProfile dentist, User user)> SeedDentistAsync()
    {
        var dentistUser = User.Create($"dentist-{Guid.NewGuid()}", $"{Guid.NewGuid()}@test.com", "hash", UserRole.Dentist, fullName: "BS Nguyễn Văn A");
        _db.Users.Add(dentistUser);
        var employee = Employee.Create(dentistUser.Id, $"DT-{Guid.NewGuid():N}");
        employee.User = dentistUser;
        var dentist = DentistProfile.Create(employee.Id, "Chỉnh nha", "N/A", 5);
        dentist.Employee = employee;
        _db.Employees.Add(employee);
        _db.DentistProfiles.Add(dentist);
        await _db.SaveChangesAsync();
        return (dentist, dentistUser);
    }

    /// <summary>Bệnh nhân mới đăng ký, chưa từng đặt lịch nên chưa có hồ sơ Patient — hồ sơ phải được
    /// tạo lười biếng (dựa vào thông tin User) trước khi tạo hội thoại, để chatbot dùng được ngay.</summary>
    [Test]
    public async Task HandleAsync_NoPatientProfileYet_LazilyCreatesPatientFromUser()
    {
        var newUser = User.Create(
            $"newpatient-{Guid.NewGuid()}", $"{Guid.NewGuid()}@test.com", "hash", UserRole.Patient,
            fullName: "Người Dùng Mới");
        newUser.UpdatePatientProfile("Người Dùng Mới", "0900000000", new DateOnly(2000, 6, 15), "Nam");
        _db.Users.Add(newUser);
        await _db.SaveChangesAsync();

        _patientRepo.GetByUserIdAsync(newUser.Id, Arg.Any<CancellationToken>()).Returns((Patient?)null);
        _userRepo.GetByIdAsync(newUser.Id, Arg.Any<CancellationToken>()).Returns(newUser);
        _patientRepo.GetFamilyMembersAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new List<Patient>());

        var result = await _handler.Handle(new StartConversationCommand(newUser.Id), CancellationToken.None);

        result.ConversationId.Should().NotBeEmpty();
        await _patientRepo.Received(1).AddAsync(
            Arg.Is<Patient>(p => p.FullName == "Người Dùng Mới"), Arg.Any<CancellationToken>());
    }

    /// <summary>Không tìm thấy cả hồ sơ Patient lẫn tài khoản User tương ứng → ném NotFoundException.</summary>
    [Test]
    public async Task HandleAsync_NoPatientAndNoUser_ThrowsNotFoundException()
    {
        var unknownUserId = Guid.NewGuid();
        _patientRepo.GetByUserIdAsync(unknownUserId, Arg.Any<CancellationToken>()).Returns((Patient?)null);
        _userRepo.GetByIdAsync(unknownUserId, Arg.Any<CancellationToken>()).Returns((User?)null);

        var act = () => _handler.Handle(new StartConversationCommand(unknownUserId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
