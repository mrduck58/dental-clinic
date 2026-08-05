using DentalClinic.API.Application.UseCases.Dentists;
using DentalClinic.API.Application.UseCases.Staff;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Infrastructure.Persistence;
using DentalClinic.API.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Handlers;

[TestFixture]
public class GetDentistDetailHandlerTests
{
    private AppDbContext _db = null!;
    private GetDentistDetailHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _handler = new GetDentistDetailHandler(
            new DentistRepository(_db), new AppointmentRepository(_db), new DentistReviewRepository(_db));
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    private async Task<DentistProfile> SeedDentistAsync(string username)
    {
        var user = User.Create(username, $"{username}-{Guid.NewGuid()}@test.com", "hash", UserRole.Dentist, fullName: "BS. Chi Tiết");
        _db.Users.Add(user);
        var employee = Employee.Create(
            user.Id,
            employeeId: $"DT-{Guid.NewGuid():N}",
            profilePictureUrl: "http://avatar.test/1.png");
        employee.User = user;
        _db.Employees.Add(employee);
        var dentist = DentistProfile.Create(
            employeeId: employee.Id,
            specialization: "Implant",
            licenseNumber: "LIC-001",
            experienceYears: 8,
            education: "Đại học Y Dược",
            biography: "Nhiều năm kinh nghiệm",
            certificateIssuedBy: "Bộ Y Tế");
        dentist.Employee = employee;
        _db.DentistProfiles.Add(dentist);
        await _db.SaveChangesAsync();
        return dentist;
    }

    private async Task<Patient> SeedPatientAsync(string username)
    {
        var user = User.Create(username, $"{username}-{Guid.NewGuid()}@test.com", "hash", UserRole.Patient);
        _db.Users.Add(user);
        var patient = Patient.Create(user.Id, new DateOnly(1990, 1, 1), "Nam");
        _db.Patients.Add(patient);
        await _db.SaveChangesAsync();
        return patient;
    }

    /// <summary>Không tìm thấy nha sĩ theo cả DentistId lẫn UserId phải trả về null, không ném lỗi.</summary>
    [Test]
    public async Task HandleAsync_DentistNotFound_ReturnsNull()
    {
        var result = await _handler.Handle(new GetDentistDetailQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    /// <summary>Tra cứu bằng đúng DentistId phải trả về đầy đủ thông tin hồ sơ nha sĩ.</summary>
    [Test]
    public async Task HandleAsync_LookupByDentistId_ReturnsFullDetail()
    {
        var dentist = await SeedDentistAsync("gd1");

        var result = await _handler.Handle(new GetDentistDetailQuery(dentist.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(dentist.Id);
        result.FullName.Should().Be("BS. Chi Tiết");
        result.Specialty.Should().Be("Implant");
        result.ProfilePictureUrl.Should().Be("http://avatar.test/1.png");
        result.YearsOfExperience.Should().Be(8);
        result.Bio.Should().Be("Nhiều năm kinh nghiệm");
        result.Education.Should().Be("Đại học Y Dược");
        result.CertificateIssuedBy.Should().Be("Bộ Y Tế");
        result.PatientCount.Should().Be(0);
    }

    /// <summary>Tra cứu bằng UserId của tài khoản liên kết (thay vì DentistId) vẫn phải trả về đúng hồ sơ nha sĩ.</summary>
    [Test]
    public async Task HandleAsync_LookupByLinkedUserId_ReturnsSameDetail()
    {
        var dentist = await SeedDentistAsync("gd2");

        var result = await _handler.Handle(new GetDentistDetailQuery(dentist.Employee.UserId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(dentist.Id);
    }

    /// <summary>Số bệnh nhân chỉ được đếm những bệnh nhân KHÁC NHAU đã hoàn tất (Completed/PendingPayment)
    /// buổi khám — lịch hẹn Pending không tính, và cùng 1 bệnh nhân khám nhiều lần chỉ tính 1.</summary>
    [Test]
    public async Task HandleAsync_PatientCount_OnlyCountsDistinctPatientsWithCompletedOrPendingPaymentVisit()
    {
        var dentist = await SeedDentistAsync("gd3");
        var patientA = await SeedPatientAsync("gd3-pa");
        var patientB = await SeedPatientAsync("gd3-pb");
        var patientC = await SeedPatientAsync("gd3-pc");

        var visit1 = Appointment.Create(patientA.Id, dentist.Id, DateTimeOffset.UtcNow.AddDays(-5));
        visit1.Complete();
        var visit2 = Appointment.Create(patientA.Id, dentist.Id, DateTimeOffset.UtcNow.AddDays(-1)); // cùng bệnh nhân, khám lần 2
        visit2.Complete();
        var visit3 = Appointment.Create(patientB.Id, dentist.Id, DateTimeOffset.UtcNow.AddDays(-2));
        visit3.EndTreatment(); // PendingPayment vẫn được tính
        var visit4 = Appointment.Create(patientC.Id, dentist.Id, DateTimeOffset.UtcNow.AddDays(1)); // vẫn Pending, chưa khám xong

        _db.Appointments.AddRange(visit1, visit2, visit3, visit4);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetDentistDetailQuery(dentist.Id), CancellationToken.None);

        result!.PatientCount.Should().Be(2); // patientA + patientB, không tính patientC
    }
}

[TestFixture]
public class GetDentistWorkingDatesHandlerTests
{
    private const int Year = 2025;
    private const int Month = 6; // June 2025 — tháng cố định để test không phụ thuộc ngày chạy thực tế

    private AppDbContext _db = null!;
    private GetDentistWorkingDatesHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _handler = new GetDentistWorkingDatesHandler(_db);
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    private static DateOnly FirstSundayOfMonth()
        => Enumerable.Range(1, DateTime.DaysInMonth(Year, Month))
            .Select(d => new DateOnly(Year, Month, d))
            .First(d => d.DayOfWeek == DayOfWeek.Sunday);

    private static DateOnly FirstWeekdayOfMonth()
        => Enumerable.Range(1, DateTime.DaysInMonth(Year, Month))
            .Select(d => new DateOnly(Year, Month, d))
            .First(d => d.DayOfWeek != DayOfWeek.Sunday);

    private async Task<DentistProfile> SeedDentistAsync(string username, string fullName = "BS. Lịch Làm Việc")
    {
        var user = User.Create(username, $"{username}-{Guid.NewGuid()}@test.com", "hash", UserRole.Dentist, fullName: fullName);
        _db.Users.Add(user);
        var employee = Employee.Create(user.Id, $"DT-{Guid.NewGuid():N}");
        employee.User = user;
        _db.Employees.Add(employee);
        var dentist = DentistProfile.Create(employee.Id, "Nha khoa tổng quát", "N/A", 5); // Shift mặc định = "morning"
        dentist.Employee = employee;
        _db.DentistProfiles.Add(dentist);
        await _db.SaveChangesAsync();
        return dentist;
    }

    /// <summary>Không tìm thấy nha sĩ theo cả DentistId lẫn UserId phải trả về danh sách rỗng.</summary>
    [Test]
    public async Task HandleAsync_DentistNotFound_ReturnsEmptyList()
    {
        var result = await _handler.Handle(new GetDentistWorkingDatesQuery(Guid.NewGuid(), Year, Month), CancellationToken.None);

        result.Should().BeEmpty();
    }

    /// <summary>Không có WorkSchedule hay lịch hẹn nào: Chủ Nhật luôn bị loại, còn ngày thường
    /// (dùng ca mặc định của nha sĩ) phải xuất hiện trong danh sách ngày còn trống.</summary>
    [Test]
    public async Task HandleAsync_NoScheduleData_ExcludesSundaysButIncludesWeekdays()
    {
        var dentist = await SeedDentistAsync("wd1");
        var sunday = FirstSundayOfMonth();
        var weekday = FirstWeekdayOfMonth();

        var result = (await _handler.Handle(new GetDentistWorkingDatesQuery(dentist.Id, Year, Month), CancellationToken.None)).ToList();

        result.Should().NotContain(sunday.ToString("yyyy-MM-dd"));
        result.Should().Contain(weekday.ToString("yyyy-MM-dd"));
    }

    /// <summary>Ngày được đánh dấu nghỉ lễ (IsHoliday) trong WorkSchedule phải bị loại khỏi kết quả
    /// dù là ngày thường và nha sĩ vốn có ca làm việc mặc định.</summary>
    [Test]
    public async Task HandleAsync_HolidayMarked_DateExcludedEvenOnWeekday()
    {
        var dentist = await SeedDentistAsync("wd2");
        var weekday = FirstWeekdayOfMonth();
        _db.WorkSchedules.Add(WorkSchedule.Create(weekday, "", "holiday", "holiday", "", "", "", true));
        await _db.SaveChangesAsync();

        var result = (await _handler.Handle(new GetDentistWorkingDatesQuery(dentist.Id, Year, Month), CancellationToken.None)).ToList();

        result.Should().NotContain(weekday.ToString("yyyy-MM-dd"));
    }

    /// <summary>Nha sĩ được phân ca "Off" riêng cho ngày đó trong WorkSchedule phải bị loại khỏi kết quả,
    /// dù mặc định nha sĩ có ca làm việc.</summary>
    [Test]
    public async Task HandleAsync_DentistAssignedOffShiftOnDate_DateExcluded()
    {
        var dentist = await SeedDentistAsync("wd3", "BS. Nghỉ Ca");
        var weekday = FirstWeekdayOfMonth();
        _db.WorkSchedules.Add(WorkSchedule.Create(weekday, "Off", "dentist", "dentist", "BS. Nghỉ Ca", "P1", "#fff", false));
        await _db.SaveChangesAsync();

        var result = (await _handler.Handle(new GetDentistWorkingDatesQuery(dentist.Id, Year, Month), CancellationToken.None)).ToList();

        result.Should().NotContain(weekday.ToString("yyyy-MM-dd"));
    }

    /// <summary>Khi hệ thống đã phân ca cho MỘT nha sĩ khác trong ngày đó nhưng KHÔNG phân cho nha sĩ
    /// đang xét, nha sĩ đang xét coi như nghỉ ngày đó — không được xuất hiện trong danh sách.</summary>
    [Test]
    public async Task HandleAsync_OtherDentistScheduledButNotThisOne_DateExcluded()
    {
        var dentist = await SeedDentistAsync("wd4", "BS. Không Được Phân Ca");
        var weekday = FirstWeekdayOfMonth();
        _db.WorkSchedules.Add(WorkSchedule.Create(weekday, "08:00-10:00", "dentist", "dentist", "BS. Người Khác", "P1", "#fff", false));
        await _db.SaveChangesAsync();

        var result = (await _handler.Handle(new GetDentistWorkingDatesQuery(dentist.Id, Year, Month), CancellationToken.None)).ToList();

        result.Should().NotContain(weekday.ToString("yyyy-MM-dd"));
    }

    /// <summary>Ngày mà toàn bộ khung giờ trong ca mặc định của nha sĩ đã bị lịch hẹn chiếm hết
    /// phải bị loại khỏi danh sách ngày còn trống.</summary>
    [Test]
    public async Task HandleAsync_FullyBookedDefaultShift_DateExcluded()
    {
        var dentist = await SeedDentistAsync("wd5");
        var patientUser = User.Create("wd5-p", $"wd5-p-{Guid.NewGuid()}@test.com", "hash", UserRole.Patient);
        _db.Users.Add(patientUser);
        var patient = Patient.Create(patientUser.Id, new DateOnly(1990, 1, 1), "Nam");
        _db.Patients.Add(patient);
        var service = Service.Create("Khám tổng quát dài", 500_000, 240, "Chiếm trọn ca sáng mặc định");
        _db.Services.Add(service);
        var weekday = FirstWeekdayOfMonth();

        // 08:00 giờ VN (UTC+7) kéo dài 240 phút -> chiếm hết 08:00-12:00, đúng khoảng ca "morning" mặc định.
        var appointmentDate = new DateTimeOffset(weekday.Year, weekday.Month, weekday.Day, 8, 0, 0, TimeSpan.FromHours(7));
        var appointment = Appointment.Create(patient.Id, dentist.Id, appointmentDate, serviceId: service.Id);
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();

        var result = (await _handler.Handle(new GetDentistWorkingDatesQuery(dentist.Id, Year, Month), CancellationToken.None)).ToList();

        result.Should().NotContain(weekday.ToString("yyyy-MM-dd"));
    }

    /// <summary>Tra cứu bằng UserId của tài khoản liên kết (thay vì DentistId) phải trả về cùng kết quả
    /// như tra cứu bằng DentistId.</summary>
    [Test]
    public async Task HandleAsync_LookupByLinkedUserId_ReturnsSameWorkingDates()
    {
        var dentist = await SeedDentistAsync("wd6");

        var byDentistId = (await _handler.Handle(new GetDentistWorkingDatesQuery(dentist.Id, Year, Month), CancellationToken.None)).ToList();
        var byUserId = (await _handler.Handle(new GetDentistWorkingDatesQuery(dentist.Employee.UserId, Year, Month), CancellationToken.None)).ToList();

        byUserId.Should().BeEquivalentTo(byDentistId);
    }
}
