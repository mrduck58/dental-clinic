using DentalClinic.API.Application.UseCases.StaffDashboard;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Handlers;

[TestFixture]
public class GetStaffPendingInvoicesHandlerTests
{
    private AppDbContext _db = null!;
    private GetStaffPendingInvoicesHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _handler = new GetStaffPendingInvoicesHandler(_db);
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
    public async Task Handle_OnlyReturnsUnpaidInvoices()
    {
        var (patient, dentist) = await SeedBasicDataAsync();
        var appt1 = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        var appt2 = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        _db.Appointments.AddRange(appt1, appt2);
        await _db.SaveChangesAsync();

        var unpaid = Invoice.Issue(appt1.Id, "INV001", [("Trám răng số 6", 1, 350_000m, null, 350_000m)], 0, PaymentMethod.Cash);
        var paid = Invoice.Issue(appt2.Id, "INV002", [("Lấy cao răng", 1, 200_000m, null, 200_000m)], 0, PaymentMethod.Cash);
        paid.MarkAsPaid(PaymentMethod.Cash);
        _db.Invoices.AddRange(unpaid, paid);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetStaffPendingInvoicesQuery(5), CancellationToken.None);

        result.Should().ContainSingle();
        result.Single().InvoiceNumber.Should().Be("INV001");
    }

    [Test]
    public async Task Handle_MapsPatientServiceNameAndAmount()
    {
        var (patient, dentist) = await SeedBasicDataAsync();
        var appt = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        _db.Appointments.Add(appt);
        await _db.SaveChangesAsync();

        var invoice = Invoice.Issue(appt.Id, "INV001", [("Trám răng số 6", 1, 350_000m, null, 350_000m)], 0, PaymentMethod.Cash);
        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync();

        var dto = (await _handler.Handle(new GetStaffPendingInvoicesQuery(5), CancellationToken.None)).Single();

        dto.PatientName.Should().Be("Trần Thị B");
        dto.ServiceName.Should().Be("Trám răng số 6");
        dto.Amount.Should().Be(350_000m);
    }

    [Test]
    public async Task Handle_OrderedByCreatedAtAscending()
    {
        var (patient, dentist) = await SeedBasicDataAsync();
        var appt1 = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        var appt2 = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        _db.Appointments.AddRange(appt1, appt2);
        await _db.SaveChangesAsync();

        var older = Invoice.Issue(appt1.Id, "INV001", [("A", 1, 100_000m, null, 100_000m)], 0, PaymentMethod.Cash);
        var newer = Invoice.Issue(appt2.Id, "INV002", [("B", 1, 100_000m, null, 100_000m)], 0, PaymentMethod.Cash);
        typeof(Invoice).GetProperty("CreatedAt")!.SetValue(older, DateTimeOffset.UtcNow.AddHours(-2));
        typeof(Invoice).GetProperty("CreatedAt")!.SetValue(newer, DateTimeOffset.UtcNow);
        _db.Invoices.AddRange(newer, older);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetStaffPendingInvoicesQuery(5), CancellationToken.None);

        result.Select(i => i.InvoiceNumber).Should().ContainInOrder("INV001", "INV002");
    }

    [Test]
    public async Task Handle_RespectsLimit()
    {
        var (patient, dentist) = await SeedBasicDataAsync();
        for (var i = 0; i < 4; i++)
        {
            var appt = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
            _db.Appointments.Add(appt);
            await _db.SaveChangesAsync();
            _db.Invoices.Add(Invoice.Issue(appt.Id, $"INV00{i}", [("A", 1, 100_000m, null, 100_000m)], 0, PaymentMethod.Cash));
        }
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetStaffPendingInvoicesQuery(2), CancellationToken.None);

        result.Should().HaveCount(2);
    }

    [Test]
    public async Task Handle_NoPendingInvoices_ReturnsEmpty()
    {
        var result = await _handler.Handle(new GetStaffPendingInvoicesQuery(5), CancellationToken.None);

        result.Should().BeEmpty();
    }

    /// <summary>Hóa đơn không có dòng chi tiết nào (Items rỗng) phải trả về ServiceName null,
    /// không được ném lỗi khi gọi FirstOrDefault trên danh sách rỗng.</summary>
    [Test]
    public async Task Handle_InvoiceWithNoItems_ReturnsNullServiceName()
    {
        var (patient, dentist) = await SeedBasicDataAsync();
        var appt = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        _db.Appointments.Add(appt);
        await _db.SaveChangesAsync();

        var invoice = Invoice.Issue(appt.Id, "INV001", [], 0, PaymentMethod.Cash);
        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync();

        var dto = (await _handler.Handle(new GetStaffPendingInvoicesQuery(5), CancellationToken.None)).Single();

        dto.ServiceName.Should().BeNull();
    }

    /// <summary>limit &lt;= 0 phải được kẹp về tối thiểu 1.</summary>
    [Test]
    public async Task Handle_LimitBelowMinimum_ClampsToOne()
    {
        var (patient, dentist) = await SeedBasicDataAsync();
        for (var i = 0; i < 3; i++)
        {
            var appt = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
            _db.Appointments.Add(appt);
            await _db.SaveChangesAsync();
            _db.Invoices.Add(Invoice.Issue(appt.Id, $"INV00{i}", [("A", 1, 100_000m, null, 100_000m)], 0, PaymentMethod.Cash));
        }
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetStaffPendingInvoicesQuery(0), CancellationToken.None);

        result.Should().ContainSingle();
    }

    /// <summary>limit vượt quá 50 phải được kẹp về tối đa 50.</summary>
    [Test]
    public async Task Handle_LimitAboveMaximum_ClampsToFifty()
    {
        var (patient, dentist) = await SeedBasicDataAsync();
        var appt = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        _db.Appointments.Add(appt);
        await _db.SaveChangesAsync();
        _db.Invoices.Add(Invoice.Issue(appt.Id, "INV001", [("A", 1, 100_000m, null, 100_000m)], 0, PaymentMethod.Cash));
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetStaffPendingInvoicesQuery(200), CancellationToken.None);

        result.Should().ContainSingle();
    }
}
