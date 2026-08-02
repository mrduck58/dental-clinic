using DentalClinic.API.Application.UseCases.Dashboard;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Handlers;

[TestFixture]
public class GetWeeklyScheduleHandlerTests
{
    private static readonly TimeZoneInfo VietnamTz = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

    private AppDbContext _db = null!;
    private GetWeeklyScheduleHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _handler = new GetWeeklyScheduleHandler(_db);
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

    private static DateOnly TodayVn() => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTz));

    [Test]
    public async Task Handle_ReturnsSevenDaysWithCorrectTodayFlag()
    {
        var result = await _handler.Handle(new GetWeeklyScheduleQuery(null), CancellationToken.None);

        result.Week.Should().HaveCount(7);
        result.Week.Should().ContainSingle(d => d.IsToday);
    }

    /// <summary>Bác sĩ có lịch hẹn đang InProgress trong ngày phải được đánh dấu IsBusy = true.</summary>
    [Test]
    public async Task Handle_DentistWithInProgressAppointment_MarkedAsBusy()
    {
        var (patient, dentist) = await SeedBasicDataAsync(dentistName: "BS. Nguyễn Minh Đức");
        var today = TodayVn();

        _db.WorkSchedules.Add(WorkSchedule.Create(
            today, "morning", "dentist", "dentist", dentist.FullName, "P101", "border-primary", false));

        var appt = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        appt.Confirm();
        appt.CheckIn();
        appt.StartTreatment();
        _db.Appointments.Add(appt);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetWeeklyScheduleQuery(today), CancellationToken.None);

        result.MorningShift.Should().ContainSingle();
        result.MorningShift.Single().StaffName.Should().Be(dentist.FullName);
        result.MorningShift.Single().IsBusy.Should().BeTrue();
    }

    /// <summary>Bác sĩ không có lịch hẹn đang khám phải được đánh dấu IsBusy = false.</summary>
    [Test]
    public async Task Handle_DentistWithoutInProgressAppointment_NotBusy()
    {
        var (_, dentist) = await SeedBasicDataAsync(dentistName: "BS. Lê Thị Phương Thảo");
        var today = TodayVn();

        _db.WorkSchedules.Add(WorkSchedule.Create(
            today, "afternoon", "dentist", "dentist", dentist.FullName, "P102", "border-secondary", false));
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetWeeklyScheduleQuery(today), CancellationToken.None);

        result.AfternoonShift.Should().ContainSingle();
        result.AfternoonShift.Single().IsBusy.Should().BeFalse();
    }

    /// <summary>Ca trực đánh dấu ngày nghỉ (IsHoliday) không được xuất hiện trong danh sách ca trực.</summary>
    [Test]
    public async Task Handle_HolidayShift_ExcludedFromResults()
    {
        var (_, dentist) = await SeedBasicDataAsync();
        var today = TodayVn();
        _db.WorkSchedules.Add(WorkSchedule.Create(
            today, "morning", "dentist", "dentist", dentist.FullName, "P101", "border-primary", true));
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetWeeklyScheduleQuery(today), CancellationToken.None);

        result.MorningShift.Should().BeEmpty();
    }
}
