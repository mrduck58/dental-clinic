using DentalClinic.API.Application.UseCases.Appointments;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Infrastructure.Persistence;
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
        _handler = new GetMyAppointmentsHandler(_patientRepo, _db);
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    /// <summary>Tài khoản chưa có hồ sơ bệnh nhân (chưa hoàn tất profile) phải trả về danh sách rỗng.</summary>
    [Test]
    public async Task HandleAsync_UserHasNoPatientProfile_ReturnsEmpty()
    {
        _patientRepo.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Patient?)null);

        var result = await _handler.HandleAsync(Guid.NewGuid());

        result.Should().BeEmpty();
    }

    /// <summary>Phải trả về lịch hẹn của chính bệnh nhân, sắp xếp mới nhất trước.</summary>
    [Test]
    public async Task HandleAsync_ReturnsOwnAppointments_OrderedByDateDescending()
    {
        var dentistUser = User.Create("ma1", $"ma1-{Guid.NewGuid()}@test.com", "hash", "Dentist");
        _db.Users.Add(dentistUser);
        var dentist = Dentist.Create(dentistUser.Id, "BS Của Tôi", "Nha khoa tổng quát", 5);
        var patient = Patient.Create("Bệnh nhân Của Tôi", new DateOnly(1990, 1, 1), "Nam");
        _db.Dentists.Add(dentist);
        _db.Patients.Add(patient);
        var older = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow.AddDays(-5));
        var newer = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow.AddDays(1));
        _db.Appointments.AddRange(older, newer);
        await _db.SaveChangesAsync();
        _patientRepo.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(patient);

        var result = (await _handler.HandleAsync(Guid.NewGuid())).ToList();

        result.Should().HaveCount(2);
        result[0].AppointmentId.Should().Be(newer.Id);
        result[0].PatientRelationship.Should().Be("Tôi");
    }

    /// <summary>Phải bao gồm cả lịch hẹn của các thành viên gia đình đặt dưới tài khoản chính.</summary>
    [Test]
    public async Task HandleAsync_IncludesFamilyMemberAppointments()
    {
        var dentistUser = User.Create("ma2", $"ma2-{Guid.NewGuid()}@test.com", "hash", "Dentist");
        _db.Users.Add(dentistUser);
        var dentist = Dentist.Create(dentistUser.Id, "BS Gia Đình", "Nha khoa tổng quát", 5);
        var primary = Patient.Create("Chủ Tài Khoản", new DateOnly(1980, 1, 1), "Nam");
        _db.Dentists.Add(dentist);
        _db.Patients.Add(primary);
        await _db.SaveChangesAsync();
        var familyMember = Patient.Create("Con Của Chủ TK", new DateOnly(2010, 1, 1), "Nữ",
            primaryPatientId: primary.Id, relationship: "Con");
        _db.Patients.Add(familyMember);
        var familyAppointment = Appointment.Create(familyMember.Id, dentist.Id, DateTimeOffset.UtcNow);
        _db.Appointments.Add(familyAppointment);
        await _db.SaveChangesAsync();
        _patientRepo.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(primary);

        var result = (await _handler.HandleAsync(Guid.NewGuid())).ToList();

        result.Should().ContainSingle(a => a.AppointmentId == familyAppointment.Id && a.PatientRelationship == "Con");
    }
}
