using DentalClinic.API.Application.UseCases.DentistDashboard;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Infrastructure.Persistence;
using DentalClinic.API.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Handlers;

/// <summary>
/// GET api/appointments/dentist/patients — bệnh nhân trong ngày của CHÍNH bác sĩ đang đăng nhập.
/// Handler chỉ ủy quyền cho <see cref="IDentistDashboardQueryService.GetMyPatientsAsync"/> (đổi userId →
/// dentistId rồi gọi lại đúng logic của <see cref="GetDentistPatientsQuery"/> trong cùng service — không
/// còn round-trip qua ISender) — nên test trực tiếp trên AppDbContext InMemory như handler kia.
/// </summary>
[TestFixture]
public class GetMyDentistPatientsHandlerTests
{
    private static readonly TimeZoneInfo VietnamTz = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

    private AppDbContext _db = null!;
    private GetMyDentistPatientsHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _handler = new GetMyDentistPatientsHandler(new DentistDashboardQueryService(_db));
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    private static DateOnly VietnamToday()
    {
        var vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTz);
        return DateOnly.FromDateTime(vietnamNow);
    }

    /// <summary>Tài khoản không có hồ sơ bác sĩ phải trả về null (controller map thành 404).</summary>
    [Test]
    public async Task HandleAsync_UserHasNoDentistProfile_ReturnsNull()
    {
        var result = await _handler.Handle(new GetMyDentistPatientsQuery(Guid.NewGuid(), null), CancellationToken.None);

        result.Should().BeNull();
    }

    /// <summary>Tìm thấy hồ sơ bác sĩ theo UserId phải trả đúng danh sách bệnh nhân đã check-in của
    /// CHÍNH bác sĩ đó vào đúng ngày truyền vào.</summary>
    [Test]
    public async Task HandleAsync_DentistFound_ReturnsPatientsForMatchingDentistAndDate()
    {
        var dentistUser = User.Create("mdp1", $"mdp1-{Guid.NewGuid()}@test.com", "hash", UserRole.Dentist);
        _db.Users.Add(dentistUser);
        var employee = Employee.Create(dentistUser.Id, $"DT-{Guid.NewGuid():N}");
        employee.User = dentistUser;
        var dentist = DentistProfile.Create(employee.Id, "Nha khoa tổng quát", "N/A", 5);
        dentist.Employee = employee;
        var patientUser = User.Create("mdp1p", $"mdp1p-{Guid.NewGuid()}@test.com", "hash", UserRole.Patient);
        _db.Users.Add(patientUser);
        var patient = Patient.Create(patientUser.Id, new DateOnly(1990, 1, 1), "Nam");
        patient.User = patientUser;
        _db.Employees.Add(employee);
        _db.DentistProfiles.Add(dentist);
        _db.Patients.Add(patient);

        var explicitDate = new DateOnly(2025, 5, 1);
        var vnStart = new DateTimeOffset(explicitDate.Year, explicitDate.Month, explicitDate.Day, 9, 0, 0, VietnamTz.BaseUtcOffset);
        var appointment = Appointment.Create(patient.Id, dentist.Id, vnStart);
        appointment.CheckIn();
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetMyDentistPatientsQuery(dentistUser.Id, explicitDate), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Date.Should().Be(explicitDate);
        result.Patients.Should().ContainSingle(p => p.AppointmentId == appointment.Id);
    }

    /// <summary>Không truyền ngày (request.Date == null) phải mặc định dùng ngày hôm nay theo giờ Việt Nam.</summary>
    [Test]
    public async Task HandleAsync_NoDateProvided_DefaultsToVietnamToday()
    {
        var dentistUser = User.Create("mdp2", $"mdp2-{Guid.NewGuid()}@test.com", "hash", UserRole.Dentist);
        _db.Users.Add(dentistUser);
        var employee = Employee.Create(dentistUser.Id, $"DT-{Guid.NewGuid():N}");
        employee.User = dentistUser;
        var dentist = DentistProfile.Create(employee.Id, "Nha khoa tổng quát", "N/A", 5);
        dentist.Employee = employee;
        _db.Employees.Add(employee);
        _db.DentistProfiles.Add(dentist);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetMyDentistPatientsQuery(dentistUser.Id, null), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Date.Should().Be(VietnamToday());
    }
}

/// <summary>
/// GET api/appointments/dentist/patients/past — bệnh nhân đã từng khám (Completed/PendingPayment)
/// của bác sĩ đang đăng nhập. Toàn bộ logic truy vấn + map nằm trong handler (không ủy quyền qua
/// sender khác), nên test trực tiếp trên AppDbContext InMemory.
/// </summary>
[TestFixture]
public class GetDentistPastPatientsHandlerTests
{
    private AppDbContext _db = null!;
    private GetDentistPastPatientsHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _handler = new GetDentistPastPatientsHandler(new DentistDashboardQueryService(_db));
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    /// <summary>Tài khoản không có hồ sơ bác sĩ phải trả về null (controller map thành 404).</summary>
    [Test]
    public async Task HandleAsync_UserHasNoDentistProfile_ReturnsNull()
    {
        var result = await _handler.Handle(new GetDentistPastPatientsQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    /// <summary>Bác sĩ chưa từng có buổi khám nào phải trả về danh sách rỗng, không ném lỗi.</summary>
    [Test]
    public async Task HandleAsync_DentistWithNoPastAppointments_ReturnsEmptyList()
    {
        var dentistUser = User.Create("dpp1", $"dpp1-{Guid.NewGuid()}@test.com", "hash", UserRole.Dentist);
        _db.Users.Add(dentistUser);
        var employee = Employee.Create(dentistUser.Id, $"DT-{Guid.NewGuid():N}");
        employee.User = dentistUser;
        var dentist = DentistProfile.Create(employee.Id, "Nha khoa tổng quát", "N/A", 5);
        dentist.Employee = employee;
        _db.Employees.Add(employee);
        _db.DentistProfiles.Add(dentist);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetDentistPastPatientsQuery(dentistUser.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    /// <summary>Chỉ hiện buổi hẹn Completed hoặc PendingPayment; Pending/Confirmed/CheckedIn/InProgress
    /// chưa kết thúc khám không được liệt kê vào lịch sử.</summary>
    [Test]
    public async Task HandleAsync_OnlyCompletedAndPendingPaymentStatusesIncluded()
    {
        var dentistUser = User.Create("dpp2", $"dpp2-{Guid.NewGuid()}@test.com", "hash", UserRole.Dentist);
        _db.Users.Add(dentistUser);
        var employee = Employee.Create(dentistUser.Id, $"DT-{Guid.NewGuid():N}");
        employee.User = dentistUser;
        var dentist = DentistProfile.Create(employee.Id, "Nha khoa tổng quát", "N/A", 5);
        dentist.Employee = employee;
        var patientUser = User.Create("dpp2p", $"dpp2p-{Guid.NewGuid()}@test.com", "hash", UserRole.Patient);
        _db.Users.Add(patientUser);
        var patient = Patient.Create(patientUser.Id, new DateOnly(1990, 1, 1), "Nam");
        patient.User = patientUser;
        _db.Employees.Add(employee);
        _db.DentistProfiles.Add(dentist);
        _db.Patients.Add(patient);
        var now = DateTimeOffset.UtcNow;

        var pending = Appointment.Create(patient.Id, dentist.Id, now);
        var confirmed = Appointment.Create(patient.Id, dentist.Id, now.AddHours(1));
        confirmed.Confirm();
        var checkedIn = Appointment.Create(patient.Id, dentist.Id, now.AddHours(2));
        checkedIn.CheckIn();
        var inProgress = Appointment.Create(patient.Id, dentist.Id, now.AddHours(3));
        inProgress.CheckIn();
        inProgress.StartTreatment();
        var completed = Appointment.Create(patient.Id, dentist.Id, now.AddHours(4));
        completed.Complete();
        var pendingPayment = Appointment.Create(patient.Id, dentist.Id, now.AddHours(5));
        pendingPayment.CheckIn();
        pendingPayment.StartTreatment();
        pendingPayment.EndTreatment();

        _db.Appointments.AddRange(pending, confirmed, checkedIn, inProgress, completed, pendingPayment);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetDentistPastPatientsQuery(dentistUser.Id), CancellationToken.None);

        result.Should().HaveCount(2);
        result!.Select(p => p.AppointmentId).Should().BeEquivalentTo([completed.Id, pendingPayment.Id]);
    }

    /// <summary>Kết quả phải sắp xếp giảm dần theo ngày hẹn — buổi khám gần nhất hiện lên đầu tiên.</summary>
    [Test]
    public async Task HandleAsync_OrdersResultsByAppointmentDateDescending()
    {
        var dentistUser = User.Create("dpp3", $"dpp3-{Guid.NewGuid()}@test.com", "hash", UserRole.Dentist);
        _db.Users.Add(dentistUser);
        var employee = Employee.Create(dentistUser.Id, $"DT-{Guid.NewGuid():N}");
        employee.User = dentistUser;
        var dentist = DentistProfile.Create(employee.Id, "Nha khoa tổng quát", "N/A", 5);
        dentist.Employee = employee;
        var patientUser = User.Create("dpp3p", $"dpp3p-{Guid.NewGuid()}@test.com", "hash", UserRole.Patient);
        _db.Users.Add(patientUser);
        var patient = Patient.Create(patientUser.Id, new DateOnly(1990, 1, 1), "Nam");
        patient.User = patientUser;
        _db.Employees.Add(employee);
        _db.DentistProfiles.Add(dentist);
        _db.Patients.Add(patient);
        var older = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow.AddDays(-10));
        older.Complete();
        var newer = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow.AddDays(-1));
        newer.Complete();
        _db.Appointments.AddRange(older, newer);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetDentistPastPatientsQuery(dentistUser.Id), CancellationToken.None);

        result![0].AppointmentId.Should().Be(newer.Id);
        result[1].AppointmentId.Should().Be(older.Id);
    }

    /// <summary>Buổi tái khám (gắn về buổi gốc qua FollowUpFromAppointmentId) khi đã Complete phải được
    /// đánh dấu IsFollowUpVisit = true; đồng thời IsNew luôn cố định false ở handler lịch sử này
    /// (khác GetDentistPatientsHandler — không tính "bệnh nhân mới" cho danh sách quá khứ).</summary>
    [Test]
    public async Task HandleAsync_CompletedFollowUpAppointment_MarkedAsFollowUpVisitAndNotNew()
    {
        var dentistUser = User.Create("dpp4", $"dpp4-{Guid.NewGuid()}@test.com", "hash", UserRole.Dentist);
        _db.Users.Add(dentistUser);
        var employee = Employee.Create(dentistUser.Id, $"DT-{Guid.NewGuid():N}");
        employee.User = dentistUser;
        var dentist = DentistProfile.Create(employee.Id, "Nha khoa tổng quát", "N/A", 5);
        dentist.Employee = employee;
        var patientUser = User.Create("dpp4p", $"dpp4p-{Guid.NewGuid()}@test.com", "hash", UserRole.Patient);
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
        followUp.Complete();
        _db.Appointments.Add(followUp);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetDentistPastPatientsQuery(dentistUser.Id), CancellationToken.None);

        var dto = result!.Single(p => p.AppointmentId == followUp.Id);
        dto.IsFollowUpVisit.Should().BeTrue();
        dto.IsNew.Should().BeFalse();
    }

    /// <summary>Bác sĩ khác (không phải bác sĩ của lịch hẹn) không được thấy lịch sử của bệnh nhân này
    /// trong danh sách của mình.</summary>
    [Test]
    public async Task HandleAsync_AppointmentBelongsToOtherDentist_IsNotIncluded()
    {
        var dentistUserA = User.Create("dpp5a", $"dpp5a-{Guid.NewGuid()}@test.com", "hash", UserRole.Dentist);
        var dentistUserB = User.Create("dpp5b", $"dpp5b-{Guid.NewGuid()}@test.com", "hash", UserRole.Dentist);
        _db.Users.AddRange(dentistUserA, dentistUserB);
        var employeeA = Employee.Create(dentistUserA.Id, $"DT-{Guid.NewGuid():N}");
        employeeA.User = dentistUserA;
        var dentistA = DentistProfile.Create(employeeA.Id, "Nha khoa tổng quát", "N/A", 5);
        dentistA.Employee = employeeA;
        var employeeB = Employee.Create(dentistUserB.Id, $"DT-{Guid.NewGuid():N}");
        employeeB.User = dentistUserB;
        var dentistB = DentistProfile.Create(employeeB.Id, "Nha khoa tổng quát", "N/A", 5);
        dentistB.Employee = employeeB;
        var patientUser = User.Create("dpp5p", $"dpp5p-{Guid.NewGuid()}@test.com", "hash", UserRole.Patient);
        _db.Users.Add(patientUser);
        var patient = Patient.Create(patientUser.Id, new DateOnly(1990, 1, 1), "Nam");
        patient.User = patientUser;
        _db.Employees.AddRange(employeeA, employeeB);
        _db.DentistProfiles.AddRange(dentistA, dentistB);
        _db.Patients.Add(patient);
        var appointmentForB = Appointment.Create(patient.Id, dentistB.Id, DateTimeOffset.UtcNow);
        appointmentForB.Complete();
        _db.Appointments.Add(appointmentForB);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetDentistPastPatientsQuery(dentistUserA.Id), CancellationToken.None);

        result.Should().BeEmpty();
    }
}
