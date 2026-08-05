using DentalClinic.API.Application.UseCases.DentistDashboard;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Handlers;

[TestFixture]
public class GetDentistPatientsHandlerTests
{
    private static readonly TimeZoneInfo VietnamTz = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

    private AppDbContext _db = null!;
    private GetDentistPatientsHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _handler = new GetDentistPatientsHandler(_db);
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    private static DateOnly VietnamToday()
    {
        var vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTz);
        return DateOnly.FromDateTime(vietnamNow);
    }

    /// <summary>Chỉ hiện bệnh nhân đã check-in trở đi (CheckedIn/InProgress/PendingPayment/Completed);
    /// Pending và Confirmed chưa check-in không được liệt kê.</summary>
    [Test]
    public async Task HandleAsync_FiltersOutNotYetCheckedInAppointments()
    {
        var dentistUser = User.Create("gp1", $"gp1-{Guid.NewGuid()}@test.com", "hash", UserRole.Dentist);
        _db.Users.Add(dentistUser);
        var employee = Employee.Create(dentistUser.Id, $"DT-{Guid.NewGuid():N}");
        employee.User = dentistUser;
        var dentist = DentistProfile.Create(employee.Id, "Nha khoa tổng quát", "N/A", 5);
        dentist.Employee = employee;
        var patient = Patient.Create(Guid.Empty, new DateOnly(1990, 1, 1), "Nam");
        _db.Employees.Add(employee);
        _db.DentistProfiles.Add(dentist);
        _db.Patients.Add(patient);
        await _db.SaveChangesAsync();

        var now = DateTimeOffset.UtcNow;
        var pending = Appointment.Create(patient.Id, dentist.Id, now);
        var confirmed = Appointment.Create(patient.Id, dentist.Id, now.AddHours(1));
        confirmed.Confirm();
        var checkedIn = Appointment.Create(patient.Id, dentist.Id, now.AddHours(2));
        checkedIn.CheckIn();
        _db.Appointments.AddRange(pending, confirmed, checkedIn);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetDentistPatientsQuery(dentist.Id, VietnamToday()), CancellationToken.None);

        result.Patients.Should().ContainSingle();
        result.Patients[0].AppointmentId.Should().Be(checkedIn.Id);
        result.TotalWaiting.Should().Be(1);
    }

    /// <summary>Tuổi bệnh nhân phải được tính đúng từ ngày sinh so với ngày hiện tại.</summary>
    [Test]
    public async Task HandleAsync_CalculatesPatientAgeCorrectly()
    {
        var dentistUser = User.Create("gp2", $"gp2-{Guid.NewGuid()}@test.com", "hash", UserRole.Dentist);
        _db.Users.Add(dentistUser);
        var employee = Employee.Create(dentistUser.Id, $"DT-{Guid.NewGuid():N}");
        employee.User = dentistUser;
        var dentist = DentistProfile.Create(employee.Id, "Nha khoa tổng quát", "N/A", 5);
        dentist.Employee = employee;
        var dob = DateOnly.FromDateTime(DateTime.Today.AddYears(-30).AddDays(1)); // chưa tới sinh nhật năm nay
        var patient = Patient.Create(Guid.Empty, dob, "Nữ");
        _db.Employees.Add(employee);
        _db.DentistProfiles.Add(dentist);
        _db.Patients.Add(patient);
        var appointment = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        appointment.CheckIn();
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetDentistPatientsQuery(dentist.Id, VietnamToday()), CancellationToken.None);

        result.Patients[0].Age.Should().Be(29);
    }

    /// <summary>Bệnh nhân chưa từng có buổi hẹn Completed nào phải được đánh dấu IsNew = true.</summary>
    [Test]
    public async Task HandleAsync_PatientWithNoCompletedVisit_IsMarkedAsNew()
    {
        var dentistUser = User.Create("gp3", $"gp3-{Guid.NewGuid()}@test.com", "hash", UserRole.Dentist);
        _db.Users.Add(dentistUser);
        var employee = Employee.Create(dentistUser.Id, $"DT-{Guid.NewGuid():N}");
        employee.User = dentistUser;
        var dentist = DentistProfile.Create(employee.Id, "Nha khoa tổng quát", "N/A", 5);
        dentist.Employee = employee;
        var patient = Patient.Create(Guid.Empty, new DateOnly(1990, 1, 1), "Nam");
        _db.Employees.Add(employee);
        _db.DentistProfiles.Add(dentist);
        _db.Patients.Add(patient);
        var appointment = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        appointment.CheckIn();
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetDentistPatientsQuery(dentist.Id, VietnamToday()), CancellationToken.None);

        result.Patients[0].IsNew.Should().BeTrue();
    }

    /// <summary>Buổi hẹn được staff check-in từ tab Tái khám (gắn về buổi gốc) phải được đánh dấu IsFollowUpVisit = true.</summary>
    [Test]
    public async Task HandleAsync_FollowUpCheckedInAppointment_MarkedAsFollowUpVisit()
    {
        var dentistUser = User.Create("gp5", $"gp5-{Guid.NewGuid()}@test.com", "hash", UserRole.Dentist);
        _db.Users.Add(dentistUser);
        var employee = Employee.Create(dentistUser.Id, $"DT-{Guid.NewGuid():N}");
        employee.User = dentistUser;
        var dentist = DentistProfile.Create(employee.Id, "Nha khoa tổng quát", "N/A", 5);
        dentist.Employee = employee;
        var patient = Patient.Create(Guid.Empty, new DateOnly(1990, 1, 1), "Nam");
        _db.Employees.Add(employee);
        _db.DentistProfiles.Add(dentist);
        _db.Patients.Add(patient);
        var original = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow.AddDays(-10));
        original.Complete();
        _db.Appointments.Add(original);
        await _db.SaveChangesAsync();
        var followUp = Appointment.CheckInFollowUp(original.Id, patient.Id, dentist.Id);
        _db.Appointments.Add(followUp);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetDentistPatientsQuery(dentist.Id, VietnamToday()), CancellationToken.None);

        result.Patients.Should().ContainSingle(p => p.AppointmentId == followUp.Id && p.IsFollowUpVisit);
    }

    /// <summary>Bệnh nhân không có số điện thoại riêng (đặt qua app) phải lấy số điện thoại từ tài khoản User liên kết.</summary>
    [Test]
    public async Task HandleAsync_PatientWithoutOwnPhoneNumber_FallsBackToUserPhoneNumber()
    {
        var dentistUser = User.Create("gp6", $"gp6-{Guid.NewGuid()}@test.com", "hash", UserRole.Dentist);
        var patientUser = User.Create("gp6-p", $"gp6-p-{Guid.NewGuid()}@test.com", "hash", UserRole.Patient);
        patientUser.UpdatePersonalProfile(patientUser.FullName, "0977000000", null);
        _db.Users.AddRange(dentistUser, patientUser);
        var employee = Employee.Create(dentistUser.Id, $"DT-{Guid.NewGuid():N}");
        employee.User = dentistUser;
        var dentist = DentistProfile.Create(employee.Id, "Nha khoa tổng quát", "N/A", 5);
        dentist.Employee = employee;
        var patient = Patient.Create(patientUser.Id, new DateOnly(1990, 1, 1), "Nam", phoneNumber: null);
        _db.Employees.Add(employee);
        _db.DentistProfiles.Add(dentist);
        _db.Patients.Add(patient);
        var appointment = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        appointment.CheckIn();
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetDentistPatientsQuery(dentist.Id, VietnamToday()), CancellationToken.None);

        result.Patients[0].Phone.Should().Be("0977000000");
    }

    /// <summary>Ngày không có lịch hẹn nào phải trả về danh sách rỗng, không ném lỗi.</summary>
    [Test]
    public async Task HandleAsync_NoAppointmentsOnDate_ReturnsEmptyList()
    {
        var dentistUser = User.Create("gp4", $"gp4-{Guid.NewGuid()}@test.com", "hash", UserRole.Dentist);
        _db.Users.Add(dentistUser);
        var employee = Employee.Create(dentistUser.Id, $"DT-{Guid.NewGuid():N}");
        employee.User = dentistUser;
        var dentist = DentistProfile.Create(employee.Id, "Nha khoa tổng quát", "N/A", 5);
        dentist.Employee = employee;
        _db.Employees.Add(employee);
        _db.DentistProfiles.Add(dentist);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetDentistPatientsQuery(dentist.Id, VietnamToday()), CancellationToken.None);

        result.Patients.Should().BeEmpty();
    }
}
