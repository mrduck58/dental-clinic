using DentalClinic.API.Application.UseCases.StaffDashboard;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Infrastructure.Persistence;
using DentalClinic.API.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Handlers;

[TestFixture]
public class GetStaffTodayAppointmentsHandlerTests
{
    private AppDbContext _db = null!;
    private GetStaffTodayAppointmentsHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _handler = new GetStaffTodayAppointmentsHandler(new StaffDashboardQueryService(_db));
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    private async Task<(Patient patient, DentistProfile dentist)> SeedBasicDataAsync(
        string dentistName = "BS. Nguyễn Văn A")
    {
        var patientUser = User.Create("p1", $"p1-{Guid.NewGuid()}@test.com", "hash", UserRole.Patient, fullName: "Trần Thị B");
        var dentistUser = User.Create("d1", $"d1-{Guid.NewGuid()}@test.com", "hash", UserRole.Dentist, fullName: dentistName);
        _db.Users.AddRange(patientUser, dentistUser);

        var employee = Employee.Create(dentistUser.Id, $"DT-{Guid.NewGuid():N}");
        employee.User = dentistUser;
        var dentist = DentistProfile.Create(employee.Id, "Nha khoa tổng quát", "N/A", 5);
        dentist.Employee = employee;
        var patient = Patient.Create(patientUser.Id, new DateOnly(1990, 1, 1), "Nữ");
        _db.Employees.Add(employee);
        _db.DentistProfiles.Add(dentist);
        _db.Patients.Add(patient);

        await _db.SaveChangesAsync();
        return (patient, dentist);
    }

    private static Appointment MakeConfirmedAt(Guid patientId, Guid dentistId, DateTimeOffset date)
    {
        var appt = Appointment.Create(patientId, dentistId, date);
        appt.Confirm();
        return appt;
    }

    [Test]
    public async Task Handle_ExcludesPendingAndCancelled()
    {
        var (patient, dentist) = await SeedBasicDataAsync();
        var pending = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        var cancelled = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        cancelled.Cancel(CancellationReason.ChangeOfPlans, null, cancelledByUserId: null, DateTimeOffset.UtcNow);
        var confirmed = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        confirmed.Confirm();
        _db.Appointments.AddRange(pending, cancelled, confirmed);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetStaffTodayAppointmentsQuery(5), CancellationToken.None);

        result.Should().ContainSingle();
        result.Single().Status.Should().Be("Confirmed");
    }

    [Test]
    public async Task Handle_MapsPatientServiceAndDentistNames()
    {
        var (patient, dentist) = await SeedBasicDataAsync(dentistName: "BS. Lê Văn D");
        var service = Service.Create("Trám răng số 6", 350_000m, 30, "Mô tả");
        _db.Services.Add(service);
        var appt = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow, serviceId: service.Id);
        appt.Confirm();
        _db.Appointments.Add(appt);
        await _db.SaveChangesAsync();

        var dto = (await _handler.Handle(new GetStaffTodayAppointmentsQuery(5), CancellationToken.None)).Single();

        dto.PatientName.Should().Be("Trần Thị B");
        dto.DentistName.Should().Be("BS. Lê Văn D");
        dto.ServiceName.Should().Be("Trám răng số 6");
    }

    [Test]
    public async Task Handle_OrderedByAppointmentTimeAscending()
    {
        var (patient, dentist) = await SeedBasicDataAsync();
        var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
        var later = new Appointment[]
        {
            MakeConfirmedAt(patient.Id, dentist.Id, new DateTimeOffset(today.Year, today.Month, today.Day, 15, 0, 0, TimeSpan.FromHours(7))),
            MakeConfirmedAt(patient.Id, dentist.Id, new DateTimeOffset(today.Year, today.Month, today.Day, 9, 0, 0, TimeSpan.FromHours(7))),
        };
        _db.Appointments.AddRange(later);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetStaffTodayAppointmentsQuery(5), CancellationToken.None);

        result.Should().BeInAscendingOrder(a => a.AppointmentDate);
    }

    [Test]
    public async Task Handle_RespectsLimit()
    {
        var (patient, dentist) = await SeedBasicDataAsync();
        for (var i = 0; i < 5; i++)
        {
            var a = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow.AddMinutes(i));
            a.Confirm();
            _db.Appointments.Add(a);
        }
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetStaffTodayAppointmentsQuery(2), CancellationToken.None);

        result.Should().HaveCount(2);
    }

    /// <summary>Lịch hẹn CheckedIn và InProgress cũng phải xuất hiện trong danh sách cần staff theo dõi,
    /// không chỉ Confirmed.</summary>
    [Test]
    public async Task Handle_IncludesCheckedInAndInProgressStatuses()
    {
        var (patient, dentist) = await SeedBasicDataAsync();
        var checkedIn = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        checkedIn.Confirm();
        checkedIn.CheckIn();
        var inProgress = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        inProgress.Confirm();
        inProgress.CheckIn();
        inProgress.StartTreatment();
        _db.Appointments.AddRange(checkedIn, inProgress);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetStaffTodayAppointmentsQuery(5), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Select(a => a.Status).Should().BeEquivalentTo(new[] { "CheckedIn", "InProgress" });
    }

    /// <summary>limit &lt;= 0 phải được kẹp về tối thiểu 1 thay vì trả về rỗng hoặc lỗi.</summary>
    [Test]
    public async Task Handle_LimitBelowMinimum_ClampsToOne()
    {
        var (patient, dentist) = await SeedBasicDataAsync();
        for (var i = 0; i < 3; i++)
        {
            var a = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow.AddMinutes(i));
            a.Confirm();
            _db.Appointments.Add(a);
        }
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetStaffTodayAppointmentsQuery(0), CancellationToken.None);

        result.Should().ContainSingle();
    }

    /// <summary>limit vượt quá 50 phải được kẹp về tối đa 50.</summary>
    [Test]
    public async Task Handle_LimitAboveMaximum_ClampsToFifty()
    {
        var (patient, dentist) = await SeedBasicDataAsync();
        var a = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        a.Confirm();
        _db.Appointments.Add(a);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetStaffTodayAppointmentsQuery(200), CancellationToken.None);

        result.Should().ContainSingle();
    }
}
