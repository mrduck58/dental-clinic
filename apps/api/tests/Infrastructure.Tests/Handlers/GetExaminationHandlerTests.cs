using DentalClinic.API.Application.DTOs.ClinicalRecords;
using DentalClinic.API.Application.UseCases.ClinicalRecords;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Infrastructure.Persistence;
using DentalClinic.API.Infrastructure.Persistence.Repositories;
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
        _handler = new GetExaminationHandler(new AppointmentRepository(_db));
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    /// <summary>Lịch hẹn không tồn tại phải trả về null thay vì ném lỗi.</summary>
    [Test]
    public async Task HandleAsync_AppointmentNotFound_ReturnsNull()
    {
        var result = await _handler.Handle(new GetExaminationQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    /// <summary>Hồ sơ khám đầy đủ phải gồm thông tin bệnh nhân, bác sĩ, chẩn đoán và đơn thuốc đã lưu.</summary>
    [Test]
    public async Task HandleAsync_AppointmentWithDiagnosisAndPrescription_ReturnsFullDto()
    {
        var dentistUser = User.Create("ex1", $"ex1-{Guid.NewGuid()}@test.com", "hash", UserRole.Dentist, fullName: "BS Khám");
        _db.Users.Add(dentistUser);
        var employee = Employee.Create(dentistUser.Id, $"DT-{Guid.NewGuid():N}");
        employee.User = dentistUser;
        var dentist = DentistProfile.Create(employee.Id, "Nha khoa tổng quát", "N/A", 5);
        dentist.Employee = employee;
        var patientUser = User.Create("pa_ex1", $"pa_ex1-{Guid.NewGuid()}@test.com", "hash", UserRole.Patient, fullName: "Bệnh nhân Khám");
        _db.Users.Add(patientUser);
        var patient = Patient.Create(patientUser.Id, new DateOnly(1990, 1, 1), "Nam");
        patient.User = patientUser;
        _db.Employees.Add(employee);
        _db.DentistProfiles.Add(dentist);
        _db.Patients.Add(patient);
        var appointment = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        appointment.StartTreatment();
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();

        var diagnosis = Diagnosis.Create(appointment.Id, "K02.1", new DiagnosisDetails("Sâu răng", null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null));
        _db.Diagnoses.Add(diagnosis);
        var prescription = Prescription.Create(appointment.Id, "Uống sau ăn");
        _db.Prescriptions.Add(prescription);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetExaminationQuery(appointment.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Patient.FullName.Should().Be("Bệnh nhân Khám");
        result.Dentist.FullName.Should().Be("BS Khám");
        result.Diagnoses.Should().ContainSingle(d => d.Description == "K02.1");
        result.Prescription.Should().NotBeNull();
        result.Prescription!.Notes.Should().Be("Uống sau ăn");
    }

    /// <summary>Buổi hẹn tái khám phải trả về đúng chuỗi buổi hẹn gốc (RelatedAppointmentIds).</summary>
    [Test]
    public async Task HandleAsync_FollowUpAppointment_ReturnsFollowUpChain()
    {
        var dentistUser = User.Create("ex2", $"ex2-{Guid.NewGuid()}@test.com", "hash", UserRole.Dentist, fullName: "BS ex2");
        _db.Users.Add(dentistUser);
        var employee = Employee.Create(dentistUser.Id, $"DT-{Guid.NewGuid():N}");
        employee.User = dentistUser;
        var dentist = DentistProfile.Create(employee.Id, "Nha khoa tổng quát", "N/A", 5);
        dentist.Employee = employee;
        var patientUser = User.Create("pa_ex2", $"pa_ex2-{Guid.NewGuid()}@test.com", "hash", UserRole.Patient, fullName: "Bệnh Nhân ex2");
        _db.Users.Add(patientUser);
        var patient = Patient.Create(patientUser.Id, new DateOnly(1990, 1, 1), "Nam");
        patient.User = patientUser;
        _db.Employees.Add(employee);
        _db.DentistProfiles.Add(dentist);
        _db.Patients.Add(patient);
        var original = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow.AddDays(-10));
        original.Complete();
        _db.Appointments.Add(original);
        await _db.SaveChangesAsync();

        var followUp = Appointment.CreateWalkIn(patient.Id, dentist.Id, DateTimeOffset.UtcNow, followUpFromAppointmentId: original.Id);
        _db.Appointments.Add(followUp);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetExaminationQuery(followUp.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.IsFollowUpVisit.Should().BeTrue();
        result.RelatedAppointmentIds.Should().Contain(original.Id);
    }
}
