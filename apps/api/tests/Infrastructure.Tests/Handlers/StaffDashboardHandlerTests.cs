using DentalClinic.API.Application.UseCases.StaffDashboard;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Handlers;

[TestFixture]
public class StaffDashboardHandlerTests
{
    private AppDbContext _db = null!;
    private StaffDashboardHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _handler = new StaffDashboardHandler(_db);
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    // ── Seed helpers ──────────────────────────────────────────────────────────

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

    // ── GetStatsAsync ─────────────────────────────────────────────────────────

    [Test]
    public async Task GetStatsAsync_CountsAppointmentsTodayExcludingCancelled()
    {
        var (patient, dentist) = await SeedBasicDataAsync();
        var confirmed = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        confirmed.Confirm();
        var cancelled = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        cancelled.Cancel();
        _db.Appointments.AddRange(confirmed, cancelled);
        await _db.SaveChangesAsync();

        var result = await _handler.GetStatsAsync();

        result.AppointmentsTodayCount.Should().Be(1);
    }

    [Test]
    public async Task GetStatsAsync_ExcludesAppointmentsOnOtherDays()
    {
        var (patient, dentist) = await SeedBasicDataAsync();
        _db.Appointments.Add(Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow.AddDays(5)));
        await _db.SaveChangesAsync();

        var result = await _handler.GetStatsAsync();

        result.AppointmentsTodayCount.Should().Be(0);
    }

    [Test]
    public async Task GetStatsAsync_WaitingCheckInCount_OnlyCountsConfirmedStatus()
    {
        var (patient, dentist) = await SeedBasicDataAsync();
        var confirmed = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        confirmed.Confirm();
        var checkedIn = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        checkedIn.Confirm();
        checkedIn.CheckIn();
        _db.Appointments.AddRange(confirmed, checkedIn);
        await _db.SaveChangesAsync();

        var result = await _handler.GetStatsAsync();

        result.WaitingCheckInCount.Should().Be(1);
    }

    [Test]
    public async Task GetStatsAsync_InProgressCount_OnlyCountsInProgressStatus()
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

        var result = await _handler.GetStatsAsync();

        result.InProgressCount.Should().Be(1);
    }

    [Test]
    public async Task GetStatsAsync_PendingInvoicesCount_OnlyCountsUnpaidInvoices()
    {
        var (patient, dentist) = await SeedBasicDataAsync();
        var appt1 = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        var appt2 = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        _db.Appointments.AddRange(appt1, appt2);
        await _db.SaveChangesAsync();

        var unpaid = Invoice.Issue(appt1.Id, "INV001", [("Khám", 1, 200_000m)], 0, PaymentMethod.Cash, PaymentType.Full, 200_000m);
        var paid = Invoice.Issue(appt2.Id, "INV002", [("Khám", 1, 300_000m)], 0, PaymentMethod.Cash, PaymentType.Full, 300_000m);
        paid.MarkAsPaid(PaymentMethod.Cash);
        _db.Invoices.AddRange(unpaid, paid);
        await _db.SaveChangesAsync();

        var result = await _handler.GetStatsAsync();

        result.PendingInvoicesCount.Should().Be(1);
    }

    // ── GetTodayAppointmentsAsync ─────────────────────────────────────────────

    [Test]
    public async Task GetTodayAppointmentsAsync_ExcludesPendingAndCancelled()
    {
        var (patient, dentist) = await SeedBasicDataAsync();
        var pending = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        var cancelled = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        cancelled.Cancel();
        var confirmed = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        confirmed.Confirm();
        _db.Appointments.AddRange(pending, cancelled, confirmed);
        await _db.SaveChangesAsync();

        var result = await _handler.GetTodayAppointmentsAsync(5);

        result.Should().ContainSingle();
        result.Single().Status.Should().Be("Confirmed");
    }

    [Test]
    public async Task GetTodayAppointmentsAsync_MapsPatientServiceAndDentistNames()
    {
        var (patient, dentist) = await SeedBasicDataAsync(dentistName: "BS. Lê Văn D");
        var service = Service.Create("Trám răng số 6", 350_000m, 30, "Mô tả");
        _db.Services.Add(service);
        var appt = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow, serviceId: service.Id);
        appt.Confirm();
        _db.Appointments.Add(appt);
        await _db.SaveChangesAsync();

        var dto = (await _handler.GetTodayAppointmentsAsync(5)).Single();

        dto.PatientName.Should().Be("Trần Thị B");
        dto.DentistName.Should().Be("BS. Lê Văn D");
        dto.ServiceName.Should().Be("Trám răng số 6");
    }

    [Test]
    public async Task GetTodayAppointmentsAsync_OrderedByAppointmentTimeAscending()
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

        var result = await _handler.GetTodayAppointmentsAsync(5);

        result.Should().BeInAscendingOrder(a => a.AppointmentDate);
    }

    [Test]
    public async Task GetTodayAppointmentsAsync_RespectsLimit()
    {
        var (patient, dentist) = await SeedBasicDataAsync();
        for (var i = 0; i < 5; i++)
        {
            var a = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow.AddMinutes(i));
            a.Confirm();
            _db.Appointments.Add(a);
        }
        await _db.SaveChangesAsync();

        var result = await _handler.GetTodayAppointmentsAsync(2);

        result.Should().HaveCount(2);
    }

    private static Appointment MakeConfirmedAt(Guid patientId, Guid dentistId, DateTimeOffset date)
    {
        var appt = Appointment.Create(patientId, dentistId, date);
        appt.Confirm();
        return appt;
    }

    // ── GetPendingInvoicesAsync ───────────────────────────────────────────────

    [Test]
    public async Task GetPendingInvoicesAsync_OnlyReturnsUnpaidInvoices()
    {
        var (patient, dentist) = await SeedBasicDataAsync();
        var appt1 = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        var appt2 = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        _db.Appointments.AddRange(appt1, appt2);
        await _db.SaveChangesAsync();

        var unpaid = Invoice.Issue(appt1.Id, "INV001", [("Trám răng số 6", 1, 350_000m)], 0, PaymentMethod.Cash, PaymentType.Full, 350_000m);
        var paid = Invoice.Issue(appt2.Id, "INV002", [("Lấy cao răng", 1, 200_000m)], 0, PaymentMethod.Cash, PaymentType.Full, 200_000m);
        paid.MarkAsPaid(PaymentMethod.Cash);
        _db.Invoices.AddRange(unpaid, paid);
        await _db.SaveChangesAsync();

        var result = await _handler.GetPendingInvoicesAsync(5);

        result.Should().ContainSingle();
        result.Single().InvoiceNumber.Should().Be("INV001");
    }

    [Test]
    public async Task GetPendingInvoicesAsync_MapsPatientServiceNameAndAmount()
    {
        var (patient, dentist) = await SeedBasicDataAsync();
        var appt = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        _db.Appointments.Add(appt);
        await _db.SaveChangesAsync();

        var invoice = Invoice.Issue(appt.Id, "INV001", [("Trám răng số 6", 1, 350_000m)], 0, PaymentMethod.Cash, PaymentType.Full, 350_000m);
        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync();

        var dto = (await _handler.GetPendingInvoicesAsync(5)).Single();

        dto.PatientName.Should().Be("Trần Thị B");
        dto.ServiceName.Should().Be("Trám răng số 6");
        dto.Amount.Should().Be(350_000m);
    }

    [Test]
    public async Task GetPendingInvoicesAsync_OrderedByCreatedAtAscending()
    {
        var (patient, dentist) = await SeedBasicDataAsync();
        var appt1 = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        var appt2 = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        _db.Appointments.AddRange(appt1, appt2);
        await _db.SaveChangesAsync();

        var older = Invoice.Issue(appt1.Id, "INV001", [("A", 1, 100_000m)], 0, PaymentMethod.Cash, PaymentType.Full, 100_000m);
        var newer = Invoice.Issue(appt2.Id, "INV002", [("B", 1, 100_000m)], 0, PaymentMethod.Cash, PaymentType.Full, 100_000m);
        typeof(Invoice).GetProperty("CreatedAt")!.SetValue(older, DateTimeOffset.UtcNow.AddHours(-2));
        typeof(Invoice).GetProperty("CreatedAt")!.SetValue(newer, DateTimeOffset.UtcNow);
        _db.Invoices.AddRange(newer, older);
        await _db.SaveChangesAsync();

        var result = await _handler.GetPendingInvoicesAsync(5);

        result.Select(i => i.InvoiceNumber).Should().ContainInOrder("INV001", "INV002");
    }

    [Test]
    public async Task GetPendingInvoicesAsync_RespectsLimit()
    {
        var (patient, dentist) = await SeedBasicDataAsync();
        for (var i = 0; i < 4; i++)
        {
            var appt = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
            _db.Appointments.Add(appt);
            await _db.SaveChangesAsync();
            _db.Invoices.Add(Invoice.Issue(appt.Id, $"INV00{i}", [("A", 1, 100_000m)], 0, PaymentMethod.Cash, PaymentType.Full, 100_000m));
        }
        await _db.SaveChangesAsync();

        var result = await _handler.GetPendingInvoicesAsync(2);

        result.Should().HaveCount(2);
    }

    [Test]
    public async Task GetPendingInvoicesAsync_NoPendingInvoices_ReturnsEmpty()
    {
        var result = await _handler.GetPendingInvoicesAsync(5);

        result.Should().BeEmpty();
    }
}
