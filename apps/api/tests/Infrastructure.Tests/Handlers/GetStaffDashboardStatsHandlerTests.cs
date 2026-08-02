using DentalClinic.API.Application.UseCases.StaffDashboard;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Handlers;

[TestFixture]
public class GetStaffDashboardStatsHandlerTests
{
    private AppDbContext _db = null!;
    private GetStaffDashboardStatsHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _handler = new GetStaffDashboardStatsHandler(_db);
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    private async Task<(Patient patient, Dentist dentist)> SeedBasicDataAsync(
        string dentistName = "BS. Nguyễn Văn A")
    {
        var patientUser = User.Create("p1", $"p1-{Guid.NewGuid()}@test.com", "hash", "Patient", fullName: "Trần Thị B");
        var dentistUser = User.Create("d1", $"d1-{Guid.NewGuid()}@test.com", "hash", "Dentist", fullName: dentistName);
        _db.Users.AddRange(patientUser, dentistUser);

        var dentist = Dentist.Create(dentistUser.Id, "Nha khoa tổng quát", 5);
        var patient = Patient.Create(patientUser.Id, new DateOnly(1990, 1, 1), "Nữ");
        _db.Dentists.Add(dentist);
        _db.Patients.Add(patient);

        await _db.SaveChangesAsync();
        return (patient, dentist);
    }

    [Test]
    public async Task Handle_CountsAppointmentsTodayExcludingCancelled()
    {
        var (patient, dentist) = await SeedBasicDataAsync();
        var confirmed = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        confirmed.Confirm();
        var cancelled = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        cancelled.Cancel();
        _db.Appointments.AddRange(confirmed, cancelled);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetStaffDashboardStatsQuery(), CancellationToken.None);

        result.AppointmentsTodayCount.Should().Be(1);
    }

    [Test]
    public async Task Handle_ExcludesAppointmentsOnOtherDays()
    {
        var (patient, dentist) = await SeedBasicDataAsync();
        _db.Appointments.Add(Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow.AddDays(5)));
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetStaffDashboardStatsQuery(), CancellationToken.None);

        result.AppointmentsTodayCount.Should().Be(0);
    }

    [Test]
    public async Task Handle_WaitingCheckInCount_OnlyCountsConfirmedStatus()
    {
        var (patient, dentist) = await SeedBasicDataAsync();
        var confirmed = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        confirmed.Confirm();
        var checkedIn = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        checkedIn.Confirm();
        checkedIn.CheckIn();
        _db.Appointments.AddRange(confirmed, checkedIn);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetStaffDashboardStatsQuery(), CancellationToken.None);

        result.WaitingCheckInCount.Should().Be(1);
    }

    [Test]
    public async Task Handle_InProgressCount_OnlyCountsInProgressStatus()
    {
        var (patient, dentist) = await SeedBasicDataAsync();
        var inProgress = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        inProgress.Confirm();
        inProgress.CheckIn();
        inProgress.StartTreatment();
        var confirmed = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        confirmed.Confirm();
        _db.Appointments.AddRange(inProgress, confirmed);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetStaffDashboardStatsQuery(), CancellationToken.None);

        result.InProgressCount.Should().Be(1);
    }

    [Test]
    public async Task Handle_PendingInvoicesCount_OnlyCountsUnpaidInvoices()
    {
        var (patient, dentist) = await SeedBasicDataAsync();
        var appt1 = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        var appt2 = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        _db.Appointments.AddRange(appt1, appt2);
        await _db.SaveChangesAsync();

        var unpaid = Invoice.Issue(appt1.Id, "INV001", [("Khám", 1, 200_000m, null, 200_000m)], 0, PaymentMethod.Cash);
        var paid = Invoice.Issue(appt2.Id, "INV002", [("Khám", 1, 300_000m, null, 300_000m)], 0, PaymentMethod.Cash);
        paid.MarkAsPaid(PaymentMethod.Cash);
        _db.Invoices.AddRange(unpaid, paid);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetStaffDashboardStatsQuery(), CancellationToken.None);

        result.PendingInvoicesCount.Should().Be(1);
    }
}
