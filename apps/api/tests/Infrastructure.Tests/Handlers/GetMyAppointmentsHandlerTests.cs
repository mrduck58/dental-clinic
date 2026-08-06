using DentalClinic.API.Application.UseCases.Booking;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Infrastructure.Persistence;
using DentalClinic.API.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Handlers;

[TestFixture]
public class GetMyAppointmentsHandlerTests
{
    private AppDbContext _db = null!;
    private IPatientRepository _patientRepo = null!;
    private GetMyAppointmentsHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _patientRepo = Substitute.For<IPatientRepository>();
        _handler = new GetMyAppointmentsHandler(_patientRepo, new AppointmentRepository(_db));
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    /// <summary>Tài khoản chưa có hồ sơ bệnh nhân (chưa hoàn tất profile) phải trả về danh sách rỗng.</summary>
    [Test]
    public async Task HandleAsync_UserHasNoPatientProfile_ReturnsEmpty()
    {
        _patientRepo.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Patient?)null);

        var result = await _handler.Handle(new GetMyAppointmentsQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeEmpty();
    }

    /// <summary>Phải trả về lịch hẹn của chính bệnh nhân, sắp xếp mới nhất trước.</summary>
    [Test]
    public async Task HandleAsync_ReturnsOwnAppointments_OrderedByDateDescending()
    {
        var dentistUser = User.Create("ma1", $"ma1-{Guid.NewGuid()}@test.com", "hash", UserRole.Dentist, fullName: "BS ma1");
        _db.Users.Add(dentistUser);
        var employee = Employee.Create(dentistUser.Id, $"DT-{Guid.NewGuid():N}");
        employee.User = dentistUser;
        var dentist = DentistProfile.Create(employee.Id, "Nha khoa tổng quát", "N/A", 5);
        dentist.Employee = employee;
        var patientUser = User.Create("pa1", $"pa1-{Guid.NewGuid()}@test.com", "hash", UserRole.Patient, fullName: "Bệnh Nhân Một");
        _db.Users.Add(patientUser);
        var patient = Patient.Create(patientUser.Id, new DateOnly(1990, 1, 1), "Nam");
        patient.User = patientUser;
        _db.Employees.Add(employee);
        _db.DentistProfiles.Add(dentist);
        _db.Patients.Add(patient);
        var older = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow.AddDays(-5));
        var newer = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow.AddDays(1));
        _db.Appointments.AddRange(older, newer);
        await _db.SaveChangesAsync();
        _patientRepo.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(patient);

        var result = (await _handler.Handle(new GetMyAppointmentsQuery(Guid.NewGuid()), CancellationToken.None)).ToList();

        result.Should().HaveCount(2);
        result[0].AppointmentId.Should().Be(newer.Id);
        result[0].PatientRelationship.Should().Be("Tôi");
    }

    /// <summary>Phải bao gồm cả lịch hẹn của các thành viên gia đình đặt dưới tài khoản chính.</summary>
    [Test]
    public async Task HandleAsync_IncludesFamilyMemberAppointments()
    {
        var dentistUser = User.Create("ma2", $"ma2-{Guid.NewGuid()}@test.com", "hash", UserRole.Dentist, fullName: "BS ma2");
        _db.Users.Add(dentistUser);
        var employee = Employee.Create(dentistUser.Id, $"DT-{Guid.NewGuid():N}");
        employee.User = dentistUser;
        var dentist = DentistProfile.Create(employee.Id, "Nha khoa tổng quát", "N/A", 5);
        dentist.Employee = employee;
        var primaryUser = User.Create("pa2", $"pa2-{Guid.NewGuid()}@test.com", "hash", UserRole.Patient, fullName: "Bệnh Nhân Hai");
        _db.Users.Add(primaryUser);
        var primary = Patient.Create(primaryUser.Id, new DateOnly(1980, 1, 1), "Nam");
        primary.User = primaryUser;
        _db.Employees.Add(employee);
        _db.DentistProfiles.Add(dentist);
        _db.Patients.Add(primary);
        await _db.SaveChangesAsync();
        var familyUser = User.Create("pa2fam", $"pa2fam-{Guid.NewGuid()}@test.com", "hash", UserRole.Patient, fullName: "Con Bệnh Nhân Hai");
        _db.Users.Add(familyUser);
        var familyMember = Patient.Create(familyUser.Id, new DateOnly(2010, 1, 1), "Nữ",
            primaryPatientId: primary.Id, relationship: "Con");
        familyMember.User = familyUser;
        _db.Patients.Add(familyMember);
        var familyAppointment = Appointment.Create(familyMember.Id, dentist.Id, DateTimeOffset.UtcNow);
        _db.Appointments.Add(familyAppointment);
        await _db.SaveChangesAsync();
        _patientRepo.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(primary);

        var result = (await _handler.Handle(new GetMyAppointmentsQuery(Guid.NewGuid()), CancellationToken.None)).ToList();

        result.Should().ContainSingle(a => a.AppointmentId == familyAppointment.Id && a.PatientRelationship == "Con");
    }
}
