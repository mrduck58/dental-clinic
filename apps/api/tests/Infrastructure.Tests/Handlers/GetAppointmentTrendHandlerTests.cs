using DentalClinic.API.Application.UseCases.Dashboard;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Handlers;

[TestFixture]
public class GetAppointmentTrendHandlerTests
{
    private static readonly TimeZoneInfo VietnamTz = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

    private AppDbContext _db = null!;
    private GetAppointmentTrendHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _handler = new GetAppointmentTrendHandler(_db);
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    private async Task<(Patient patient, Dentist dentist)> SeedBasicDataAsync(
        string dentistName = "BS. Nguyễn Văn A", string specialization = "Nha khoa tổng quát")
    {
        var patientUser = User.Create("p1", $"p1-{Guid.NewGuid()}@test.com", "hash", "Patient", fullName: "Trần Thị B");
        var dentistUser = User.Create("d1", $"d1-{Guid.NewGuid()}@test.com", "hash", "Dentist", fullName: dentistName);
        _db.Users.AddRange(patientUser, dentistUser);

        var dentist = Dentist.Create(dentistUser.Id, specialization, 5);
        var patient = Patient.Create(patientUser.Id, new DateOnly(1990, 1, 1), "Nữ");
        _db.Dentists.Add(dentist);
        _db.Patients.Add(patient);

        await _db.SaveChangesAsync();
        return (patient, dentist);
    }

    private static DateTimeOffset ToVn(DateOnly date) => new(date.Year, date.Month, date.Day, 0, 0, 0, VietnamTz.BaseUtcOffset);

    private static DateOnly TodayVn() => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTz));

    private static (DateOnly Start, DateOnly End) ThisWeek()
    {
        var today = TodayVn();
        var dow = (int)today.DayOfWeek;
        var daysFromMonday = dow == 0 ? 6 : dow - 1;
        var start = today.AddDays(-daysFromMonday);
        return (start, start.AddDays(7));
    }

    [Test]
    public async Task Handle_InvalidRange_ThrowsValidationException()
    {
        Func<Task> act = async () => await _handler.Handle(new GetAppointmentTrendQuery("century"), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task Handle_WeekRange_ReturnsSevenBuckets()
    {
        var result = await _handler.Handle(new GetAppointmentTrendQuery("week"), CancellationToken.None);

        result.Points.Should().HaveCount(7);
    }

    /// <summary>Tổng số lịch hẹn trong các bucket phải bằng tổng lịch hẹn hợp lệ (không hủy) trong tuần.</summary>
    [Test]
    public async Task Handle_WeekRange_BucketsSumMatchesTotalAppointments()
    {
        var (patient, dentist) = await SeedBasicDataAsync();
        var (weekStart, _) = ThisWeek();
        _db.Appointments.AddRange(
            Appointment.Create(patient.Id, dentist.Id, ToVn(weekStart).AddHours(9)),
            Appointment.Create(patient.Id, dentist.Id, ToVn(weekStart).AddHours(10)),
            Appointment.Create(patient.Id, dentist.Id, ToVn(weekStart.AddDays(2)).AddHours(9)));
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetAppointmentTrendQuery("week"), CancellationToken.None);

        result.Points.Sum(p => p.Count).Should().Be(3);
    }

    [Test]
    public async Task Handle_YearRange_ReturnsTwelveBuckets()
    {
        var result = await _handler.Handle(new GetAppointmentTrendQuery("year"), CancellationToken.None);

        result.Points.Should().HaveCount(12);
    }

    /// <summary>Range "month" phải chia theo tuần trong tháng (số bucket = số tuần cần để phủ hết
    /// các ngày trong tháng hiện tại, tối đa 5 khi tháng có 31 ngày).</summary>
    [Test]
    public async Task Handle_MonthRange_ReturnsWeeklyBucketsCoveringWholeMonth()
    {
        var today = TodayVn();
        var daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);
        var expectedBuckets = (int)Math.Ceiling(daysInMonth / 7.0);

        var result = await _handler.Handle(new GetAppointmentTrendQuery("month"), CancellationToken.None);

        result.Range.Should().Be("month");
        result.Points.Should().HaveCount(expectedBuckets);
        result.Points.Sum(p => p.Count).Should().Be(0);
    }

    /// <summary>Không truyền range (null) phải mặc định về "week" thay vì ném lỗi.</summary>
    [Test]
    public async Task Handle_NullRange_DefaultsToWeek()
    {
        var result = await _handler.Handle(new GetAppointmentTrendQuery(null), CancellationToken.None);

        result.Range.Should().Be("week");
        result.Points.Should().HaveCount(7);
    }
}
