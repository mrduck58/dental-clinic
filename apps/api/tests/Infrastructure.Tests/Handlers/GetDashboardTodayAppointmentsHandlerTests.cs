using DentalClinic.API.Application.UseCases.Dashboard;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Handlers;

[TestFixture]
public class GetDashboardTodayAppointmentsHandlerTests
{
    private AppDbContext _db = null!;
    private GetDashboardTodayAppointmentsHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _handler = new GetDashboardTodayAppointmentsHandler(_db);
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

    [Test]
    public async Task Handle_OnlyReturnsAppointmentsScheduledToday()
    {
        var (patient, dentist) = await SeedBasicDataAsync();
        _db.Appointments.AddRange(
            Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow),
            Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow.AddDays(3)));
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetDashboardTodayAppointmentsQuery(1, 10), CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle();
    }

    [Test]
    public async Task Handle_RespectsPagination()
    {
        var (patient, dentist) = await SeedBasicDataAsync();
        for (var i = 0; i < 5; i++)
            _db.Appointments.Add(Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow.AddMinutes(i)));
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetDashboardTodayAppointmentsQuery(2, 2), CancellationToken.None);

        result.TotalCount.Should().Be(5);
        result.Items.Should().HaveCount(2);
        result.TotalPages.Should().Be(3);
    }

    /// <summary>page &lt;= 0 phải được kẹp về trang 1 thay vì gây lỗi Skip âm.</summary>
    [Test]
    public async Task Handle_PageBelowOne_ClampsToFirstPage()
    {
        var (patient, dentist) = await SeedBasicDataAsync();
        _db.Appointments.Add(Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow));
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetDashboardTodayAppointmentsQuery(0, 10), CancellationToken.None);

        result.Page.Should().Be(1);
        result.Items.Should().ContainSingle();
    }

    /// <summary>pageSize vượt quá 100 phải được kẹp về tối đa 100.</summary>
    [Test]
    public async Task Handle_PageSizeAboveMaximum_ClampsToOneHundred()
    {
        var result = await _handler.Handle(new GetDashboardTodayAppointmentsQuery(1, 1000), CancellationToken.None);

        result.PageSize.Should().Be(100);
    }
}
