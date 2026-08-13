using DentalClinic.API.Application.UseCases.Reminders;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Infrastructure.Persistence;
using DentalClinic.API.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Handlers;

[TestFixture]
public class GetMedicationRemindersHandlerTests
{
    private AppDbContext _db = null!;
    private GetMedicationRemindersHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _handler = new GetMedicationRemindersHandler(new PatientRepository(_db), new PrescriptionItemRepository(_db));
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    /// <summary>Seed một bệnh nhân + lịch hẹn + đơn thuốc với 1 dòng thuốc đủ dữ liệu sinh lịch nhắc.</summary>
    private async Task<(Patient patient, PrescriptionItem item)> SeedPatientWithPrescriptionItemAsync(
        DateOnly startDate, int durationDays, int timesPerDay, Patient? appointmentPatient = null)
    {
        var patientUser = User.Create("med-p", $"med-p-{Guid.NewGuid()}@test.com", "hash", UserRole.Patient);
        var dentistUser = User.Create("med-d", $"med-d-{Guid.NewGuid()}@test.com", "hash", UserRole.Dentist);
        _db.Users.AddRange(patientUser, dentistUser);
        var patient = Patient.Create(patientUser.Id, new DateOnly(1990, 1, 1), "Nam");
        var employee = Employee.Create(dentistUser.Id, $"DT-{Guid.NewGuid():N}");
        employee.User = dentistUser;
        var dentist = DentistProfile.Create(employee.Id, "Nha khoa tổng quát", "N/A", 5);
        dentist.Employee = employee;
        _db.Patients.Add(patient);
        _db.Employees.Add(employee);
        _db.DentistProfiles.Add(dentist);

        var appointmentPatientEntity = appointmentPatient ?? patient;
        var appointment = Appointment.Create(appointmentPatientEntity.Id, dentist.Id, DateTimeOffset.UtcNow);
        _db.Appointments.Add(appointment);

        var prescription = Prescription.Create(appointment.Id);
        _db.Prescriptions.Add(prescription);

        var item = PrescriptionItem.Create(
            prescription.Id, "Paracetamol", "500mg", 10, "viên", "Uống sau ăn",
            timesPerDay: timesPerDay, durationDays: durationDays, startDate: startDate);
        _db.PrescriptionItems.Add(item);

        await _db.SaveChangesAsync();
        return (patient, item);
    }

    /// <summary>Tài khoản không có hồ sơ bệnh nhân phải trả về danh sách rỗng.</summary>
    [Test]
    public async Task HandleAsync_UserHasNoPatientProfile_ReturnsEmpty()
    {
        var result = await _handler.Handle(
            new GetMedicationRemindersQuery(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow)),
            CancellationToken.None);

        result.Should().BeEmpty();
    }

    /// <summary>Ngày truy vấn nằm trước khoảng StartDate..StartDate+DurationDays phải bị loại khỏi kết quả.</summary>
    [Test]
    public async Task HandleAsync_DateBeforeWindow_ExcludesItem()
    {
        var start = new DateOnly(2026, 8, 10);
        var (patient, _) = await SeedPatientWithPrescriptionItemAsync(start, durationDays: 5, timesPerDay: 1);

        var result = await _handler.Handle(
            new GetMedicationRemindersQuery(patient.UserId, start.AddDays(-1)),
            CancellationToken.None);

        result.Should().BeEmpty();
    }

    /// <summary>Ngày truy vấn nằm sau khoảng StartDate..StartDate+DurationDays (đã hết đợt uống) phải bị loại.</summary>
    [Test]
    public async Task HandleAsync_DateAfterWindow_ExcludesItem()
    {
        var start = new DateOnly(2026, 8, 10);
        var (patient, _) = await SeedPatientWithPrescriptionItemAsync(start, durationDays: 5, timesPerDay: 1);

        var result = await _handler.Handle(
            new GetMedicationRemindersQuery(patient.UserId, start.AddDays(5)), // == end, đã hết hạn (exclusive)
            CancellationToken.None);

        result.Should().BeEmpty();
    }

    /// <summary>Ngày truy vấn nằm trong khoảng sử dụng thuốc phải trả về nhắc nhở với đúng số lần/ngày và quan hệ "Tôi".</summary>
    [Test]
    public async Task HandleAsync_DateWithinWindow_ReturnsRemindersWithGeneratedTimeSlots()
    {
        var start = new DateOnly(2026, 8, 10);
        var (patient, item) = await SeedPatientWithPrescriptionItemAsync(start, durationDays: 5, timesPerDay: 3);

        var result = await _handler.Handle(
            new GetMedicationRemindersQuery(patient.UserId, start.AddDays(2)),
            CancellationToken.None);

        result.Should().HaveCount(3);
        result.Select(r => r.Time).Should().BeEquivalentTo(new[] { "07:00", "14:00", "21:00" });
        result.Should().OnlyContain(r => r.PrescriptionItemId == item.Id);
        result.Should().OnlyContain(r => r.PatientId == patient.Id);
        result.Should().OnlyContain(r => r.PatientRelationship == "Tôi");
        result.Should().OnlyContain(r => r.MedicineName == "Paracetamol" && r.Dosage == "500mg");
    }

    /// <summary>Uống 1 lần/ngày phải sinh đúng 1 khung giờ mặc định "08:00".</summary>
    [Test]
    public async Task HandleAsync_TimesPerDayOne_ReturnsSingleDefaultTimeSlot()
    {
        var start = new DateOnly(2026, 8, 10);
        var (patient, _) = await SeedPatientWithPrescriptionItemAsync(start, durationDays: 5, timesPerDay: 1);

        var result = await _handler.Handle(
            new GetMedicationRemindersQuery(patient.UserId, start),
            CancellationToken.None);

        result.Should().ContainSingle();
        result[0].Time.Should().Be("08:00");
    }

    /// <summary>Đơn thuốc của thành viên gia đình (liên kết qua PrimaryPatientId) phải được gộp vào lịch nhắc
    /// của bệnh nhân chính, với quan hệ hiển thị đúng theo Relationship đã khai báo.</summary>
    [Test]
    public async Task HandleAsync_FamilyMemberPrescription_IncludedWithFamilyRelationship()
    {
        var primaryUser = User.Create("fam-primary", $"fam-primary-{Guid.NewGuid()}@test.com", "hash", UserRole.Patient);
        _db.Users.Add(primaryUser);
        var primaryPatient = Patient.Create(primaryUser.Id, new DateOnly(1985, 1, 1), "Nữ");
        _db.Patients.Add(primaryPatient);
        await _db.SaveChangesAsync();

        var familyMemberUser = User.Create("fam-child", $"fam-child-{Guid.NewGuid()}@test.com", "hash", UserRole.Patient);
        _db.Users.Add(familyMemberUser);
        var familyMember = Patient.Create(
            familyMemberUser.Id, new DateOnly(2015, 1, 1), "Nam",
            primaryPatientId: primaryPatient.Id, relationship: "Con");
        _db.Patients.Add(familyMember);
        await _db.SaveChangesAsync();

        var start = new DateOnly(2026, 8, 10);
        var (_, item) = await SeedPatientWithPrescriptionItemAsync(
            start, durationDays: 5, timesPerDay: 1, appointmentPatient: familyMember);

        var result = await _handler.Handle(
            new GetMedicationRemindersQuery(primaryUser.Id, start.AddDays(1)),
            CancellationToken.None);

        result.Should().ContainSingle();
        result[0].PrescriptionItemId.Should().Be(item.Id);
        result[0].PatientId.Should().Be(familyMember.Id);
        result[0].PatientRelationship.Should().Be("Con");
    }

    /// <summary>Đơn thuốc thiếu dữ liệu (không có TimesPerDay/DurationDays/StartDate) sẽ không được
    /// repository trả về nên không sinh nhắc nhở.</summary>
    [Test]
    public async Task HandleAsync_ItemMissingScheduleData_IsExcludedFromReminders()
    {
        var patientUser = User.Create("med-p2", $"med-p2-{Guid.NewGuid()}@test.com", "hash", UserRole.Patient);
        var dentistUser = User.Create("med-d2", $"med-d2-{Guid.NewGuid()}@test.com", "hash", UserRole.Dentist);
        _db.Users.AddRange(patientUser, dentistUser);
        var patient = Patient.Create(patientUser.Id, new DateOnly(1990, 1, 1), "Nam");
        var employee = Employee.Create(dentistUser.Id, $"DT-{Guid.NewGuid():N}");
        employee.User = dentistUser;
        var dentist = DentistProfile.Create(employee.Id, "Nha khoa tổng quát", "N/A", 5);
        dentist.Employee = employee;
        _db.Patients.Add(patient);
        _db.Employees.Add(employee);
        _db.DentistProfiles.Add(dentist);
        var appointment = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        _db.Appointments.Add(appointment);
        var prescription = Prescription.Create(appointment.Id);
        _db.Prescriptions.Add(prescription);
        // Không có TimesPerDay/DurationDays -> bác sĩ không kê lịch nhắc thật.
        var item = PrescriptionItem.Create(prescription.Id, "Amoxicillin", "250mg", 10, "viên", "Uống sau ăn");
        _db.PrescriptionItems.Add(item);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(
            new GetMedicationRemindersQuery(patient.UserId, DateOnly.FromDateTime(DateTime.UtcNow)),
            CancellationToken.None);

        result.Should().BeEmpty();
    }
}
