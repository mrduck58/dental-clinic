using DentalClinic.API.Application.UseCases.DentistDashboard;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Infrastructure.Persistence;
using DentalClinic.API.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Handlers;

[TestFixture]
public class DentistDashboardHandlerTests
{
    private static readonly TimeZoneInfo VietnamTz = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

    private AppDbContext _db = null!;
    private DentistDashboardHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _handler = new DentistDashboardHandler(new DentistDashboardQueryService(_db));
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    private static DateOnly VietnamToday()
    {
        var vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTz);
        return DateOnly.FromDateTime(vietnamNow);
    }

    /// <summary>Không tìm thấy bác sĩ ứng với userId (tài khoản không phải Dentist) phải trả về dashboard rỗng, không ném lỗi.</summary>
    [Test]
    public async Task HandleAsync_UserIsNotADentist_ReturnsEmptyDashboard()
    {
        var result = await _handler.Handle(new GetDentistDashboardQuery(Guid.NewGuid()), CancellationToken.None);

        result.TotalPatientsToday.Should().Be(0);
        result.UpcomingPatients.Should().BeEmpty();
        result.WeekShifts.Total.Should().Be(0);
    }

    /// <summary>Lịch hẹn hôm nay với các trạng thái khác nhau phải được đếm đúng vào từng nhóm
    /// (chờ, đang khám, hoàn thành), còn Pending/Cancelled không được tính.</summary>
    [Test]
    public async Task HandleAsync_TodayAppointmentsWithVariousStatuses_CountsCorrectly()
    {
        var dentistUser = User.Create("dd1", "dd1@test.com", "hash", UserRole.Dentist);
        _db.Users.Add(dentistUser);
        var employee = Employee.Create(dentistUser.Id, $"DT-{Guid.NewGuid():N}");
        employee.User = dentistUser;
        var dentist = DentistProfile.Create(employee.Id, "Nha khoa tổng quát", "N/A", 5);
        dentist.Employee = employee;
        _db.Employees.Add(employee);
        _db.DentistProfiles.Add(dentist);
        var patient = Patient.Create(Guid.Empty, new DateOnly(1990, 1, 1), "Nam");
        _db.Patients.Add(patient);
        await _db.SaveChangesAsync();

        var now = DateTimeOffset.UtcNow;
        var confirmed = Appointment.Create(patient.Id, dentist.Id, now.AddHours(1));
        confirmed.Confirm();
        var inProgress = Appointment.Create(patient.Id, dentist.Id, now);
        inProgress.StartTreatment();
        var completed = Appointment.Create(patient.Id, dentist.Id, now.AddHours(-1));
        completed.Complete();
        var pending = Appointment.Create(patient.Id, dentist.Id, now.AddHours(2));
        var cancelled = Appointment.Create(patient.Id, dentist.Id, now.AddHours(3));
        cancelled.Cancel(CancellationReason.ChangeOfPlans, null, cancelledByUserId: null, DateTimeOffset.UtcNow);
        _db.Appointments.AddRange(confirmed, inProgress, completed, pending, cancelled);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetDentistDashboardQuery(dentistUser.Id), CancellationToken.None);

        result.TotalPatientsToday.Should().Be(3); // confirmed + inProgress + completed
        result.TotalWaiting.Should().Be(1);       // confirmed
        result.TotalInProgress.Should().Be(1);    // inProgress
        result.TotalCompleted.Should().Be(1);     // completed
    }

    /// <summary>Danh sách bệnh nhân sắp tới chỉ gồm InProgress/CheckedIn/Confirmed, tối đa 5 người.</summary>
    [Test]
    public async Task HandleAsync_UpcomingPatients_OnlyIncludesActiveStatusesUpToFive()
    {
        var dentistUser = User.Create("dd2", "dd2@test.com", "hash", UserRole.Dentist);
        _db.Users.Add(dentistUser);
        var employee = Employee.Create(dentistUser.Id, $"DT-{Guid.NewGuid():N}");
        employee.User = dentistUser;
        var dentist = DentistProfile.Create(employee.Id, "Nha khoa tổng quát", "N/A", 5);
        dentist.Employee = employee;
        _db.Employees.Add(employee);
        _db.DentistProfiles.Add(dentist);
        var patient = Patient.Create(Guid.Empty, new DateOnly(1992, 2, 2), "Nữ");
        _db.Patients.Add(patient);
        await _db.SaveChangesAsync();

        var now = DateTimeOffset.UtcNow;
        for (var i = 0; i < 7; i++)
        {
            var appt = Appointment.Create(patient.Id, dentist.Id, now.AddMinutes(i));
            appt.Confirm();
            _db.Appointments.Add(appt);
        }
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetDentistDashboardQuery(dentistUser.Id), CancellationToken.None);

        result.UpcomingPatients.Should().HaveCount(5);
        result.UpcomingPatients.Should().OnlyContain(p => p.Status == "waiting");
    }

    /// <summary>Ca làm việc trong tuần phải được đếm đúng theo buổi (sáng/chiều/tối) dựa trên tên bác sĩ khớp với lịch làm việc.</summary>
    [Test]
    public async Task HandleAsync_WeekSchedules_CountsShiftsByPeriodCorrectly()
    {
        var dentistUser = User.Create("dd3", "dd3@test.com", "hash", UserRole.Dentist);
        _db.Users.Add(dentistUser);
        var employee = Employee.Create(dentistUser.Id, $"DT-{Guid.NewGuid():N}");
        employee.User = dentistUser;
        var dentist = DentistProfile.Create(employee.Id, "Nha khoa tổng quát", "N/A", 5);
        dentist.Employee = employee;
        _db.Employees.Add(employee);
        _db.DentistProfiles.Add(dentist);
        await _db.SaveChangesAsync();

        var today = VietnamToday();
        var dow = (int)today.DayOfWeek;
        var daysFromMon = dow == 0 ? 6 : dow - 1;
        var weekStart = today.AddDays(-daysFromMon);

        _db.WorkSchedules.Add(WorkSchedule.Create(weekStart, "08:00-10:00", "dentist", "dentist", dentist.FullName, "P101", "border-primary", false));
        _db.WorkSchedules.Add(WorkSchedule.Create(weekStart.AddDays(1), "13:30-15:30", "dentist", "dentist", dentist.FullName, "P101", "border-primary", false));
        _db.WorkSchedules.Add(WorkSchedule.Create(weekStart.AddDays(2), "17:30-19:30", "dentist", "dentist", dentist.FullName, "P101", "border-primary", false));
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetDentistDashboardQuery(dentistUser.Id), CancellationToken.None);

        result.WeekShifts.Total.Should().Be(3);
        result.WeekShifts.Morning.Should().Be(1);
        result.WeekShifts.Afternoon.Should().Be(1);
        result.WeekShifts.Evening.Should().Be(1);
    }

    /// <summary>Ca làm việc đánh dấu ngày lễ (IsHoliday) không được tính vào lịch làm việc trong tuần.</summary>
    [Test]
    public async Task HandleAsync_HolidaySchedule_ExcludedFromWeekShifts()
    {
        var dentistUser = User.Create("dd4", "dd4@test.com", "hash", UserRole.Dentist);
        _db.Users.Add(dentistUser);
        var employee = Employee.Create(dentistUser.Id, $"DT-{Guid.NewGuid():N}");
        employee.User = dentistUser;
        var dentist = DentistProfile.Create(employee.Id, "Nha khoa tổng quát", "N/A", 5);
        dentist.Employee = employee;
        _db.Employees.Add(employee);
        _db.DentistProfiles.Add(dentist);
        await _db.SaveChangesAsync();

        var today = VietnamToday();
        _db.WorkSchedules.Add(WorkSchedule.Create(today, "08:00-10:00", "dentist", "dentist", dentist.FullName, "P101", "border-primary", isHoliday: true));
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetDentistDashboardQuery(dentistUser.Id), CancellationToken.None);

        result.WeekShifts.Total.Should().Be(0);
        result.TodayShifts.Should().BeEmpty();
    }

    /// <summary>Ca làm việc hôm nay của bác sĩ phải được liệt kê đúng nhãn giờ và phòng, xếp theo thứ tự thời gian.</summary>
    [Test]
    public async Task HandleAsync_TodaySchedules_ReturnsOrderedShiftsWithRoom()
    {
        var dentistUser = User.Create("dd5", "dd5@test.com", "hash", UserRole.Dentist);
        _db.Users.Add(dentistUser);
        var employee = Employee.Create(dentistUser.Id, $"DT-{Guid.NewGuid():N}");
        employee.User = dentistUser;
        var dentist = DentistProfile.Create(employee.Id, "Nha khoa tổng quát", "N/A", 5);
        dentist.Employee = employee;
        _db.Employees.Add(employee);
        _db.DentistProfiles.Add(dentist);
        await _db.SaveChangesAsync();

        var today = VietnamToday();
        _db.WorkSchedules.Add(WorkSchedule.Create(today, "13:30-15:30", "dentist", "dentist", dentist.FullName, "P202", "border-primary", false));
        _db.WorkSchedules.Add(WorkSchedule.Create(today, "08:00-10:00", "dentist", "dentist", dentist.FullName, "P101", "border-primary", false));
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetDentistDashboardQuery(dentistUser.Id), CancellationToken.None);

        result.TodayShifts.Should().HaveCount(2);
        result.TodayShifts[0].Room.Should().Be("P101"); // ca sáng phải đứng trước ca chiều
        result.TodayShifts[1].Room.Should().Be("P202");
    }
}
