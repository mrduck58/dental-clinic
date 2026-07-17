using DentalClinic.API.Application.UseCases.Appointments;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Handlers;

[TestFixture]
public class GetExaminationHandlerTests
{
    private AppDbContext _db = null!;
    private GetExaminationHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _handler = new GetExaminationHandler(_db);
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    /// <summary>Lịch hẹn không tồn tại phải trả về null thay vì ném lỗi.</summary>
    [Test]
    public async Task HandleAsync_AppointmentNotFound_ReturnsNull()
    {
        var result = await _handler.HandleAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    /// <summary>Hồ sơ khám đầy đủ phải gồm thông tin bệnh nhân, bác sĩ, chẩn đoán và đơn thuốc đã lưu.</summary>
    [Test]
    public async Task HandleAsync_AppointmentWithDiagnosisAndPrescription_ReturnsFullDto()
    {
        var dentistUser = User.Create("ex1", $"ex1-{Guid.NewGuid()}@test.com", "hash", "Dentist");
        _db.Users.Add(dentistUser);
        var dentist = Dentist.Create(dentistUser.Id, "Nha khoa tổng quát", 5);
        var patient = Patient.Create(Guid.Empty, new DateOnly(1990, 1, 1), "Nam");
        _db.Dentists.Add(dentist);
        _db.Patients.Add(patient);
        var appointment = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        appointment.StartTreatment();
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();

        var diagnosis = Diagnosis.Create(appointment.Id, "K02.1", "Sâu răng", null, null, null, null, null, null, null, null, null);
        _db.Diagnoses.Add(diagnosis);
        var prescription = Prescription.Create(appointment.Id, "Uống sau ăn");
        _db.Prescriptions.Add(prescription);
        await _db.SaveChangesAsync();

        var result = await _handler.HandleAsync(appointment.Id);

        result.Should().NotBeNull();
        result!.Patient.FullName.Should().Be("Bệnh nhân Khám");
        result.Dentist.FullName.Should().Be("BS Khám");
        result.Diagnoses.Should().ContainSingle(d => d.DiagnosisCode == "K02.1");
        result.Prescription.Should().NotBeNull();
        result.Prescription!.Notes.Should().Be("Uống sau ăn");
    }

    /// <summary>Buổi hẹn tái khám phải trả về đúng chuỗi buổi hẹn gốc (RelatedAppointmentIds).</summary>
    [Test]
    public async Task HandleAsync_FollowUpAppointment_ReturnsFollowUpChain()
    {
        var dentistUser = User.Create("ex2", $"ex2-{Guid.NewGuid()}@test.com", "hash", "Dentist");
        _db.Users.Add(dentistUser);
        var dentist = Dentist.Create(dentistUser.Id, "Nha khoa tổng quát", 5);
        var patient = Patient.Create(Guid.Empty, new DateOnly(1990, 1, 1), "Nam");
        _db.Dentists.Add(dentist);
        _db.Patients.Add(patient);
        var original = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow.AddDays(-10));
        original.Complete();
        _db.Appointments.Add(original);
        await _db.SaveChangesAsync();

        var followUp = Appointment.CheckInFollowUp(original.Id, patient.Id, dentist.Id);
        _db.Appointments.Add(followUp);
        await _db.SaveChangesAsync();

        var result = await _handler.HandleAsync(followUp.Id);

        result.Should().NotBeNull();
        result!.IsFollowUpVisit.Should().BeTrue();
        result.RelatedAppointmentIds.Should().Contain(original.Id);
    }
}
