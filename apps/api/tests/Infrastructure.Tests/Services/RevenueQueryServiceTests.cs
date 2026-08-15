using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Application.Interfaces;
using DentalClinic.API.Infrastructure.Persistence;
using DentalClinic.API.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Services;

[TestFixture]
public class RevenueQueryServiceTests
{
    private AppDbContext _db = null!;
    private RevenueQueryService _sut = null!;

    private static readonly DateOnly PeriodFrom = new(2026, 8, 1);
    private static readonly DateOnly PeriodTo = new(2026, 8, 31);

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _sut = new RevenueQueryService(_db);
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    private async Task<(Patient patient, DentistProfile dentist, Appointment appointment)> SeedAppointmentAsync()
    {
        var patientUser = User.Create("rev-p", $"rev-p-{Guid.NewGuid()}@test.com", "hash", UserRole.Patient);
        var dentistUser = User.Create("rev-d", $"rev-d-{Guid.NewGuid()}@test.com", "hash", UserRole.Dentist);
        _db.Users.AddRange(patientUser, dentistUser);
        var patient = Patient.Create(patientUser.Id, new DateOnly(1990, 1, 1), "Nam");
        var employee = Employee.Create(dentistUser.Id, $"DT-{Guid.NewGuid():N}");
        employee.User = dentistUser;
        var dentist = DentistProfile.Create(employee.Id, "Nha khoa tổng quát", "N/A", 5);
        dentist.Employee = employee;
        _db.Patients.Add(patient);
        _db.Employees.Add(employee);
        _db.DentistProfiles.Add(dentist);
        var appointment = Appointment.Create(patient.Id, dentist.Id, new DateTimeOffset(2026, 8, 10, 8, 0, 0, TimeSpan.Zero));
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();
        return (patient, dentist, appointment);
    }

    private Invoice IssuePaidInvoice(Guid appointmentId, string serviceName, decimal unitPrice, DateTimeOffset paymentDate, decimal? amountCollected = null)
    {
        var invoice = Invoice.Issue(
            appointmentId, $"INV-{Guid.NewGuid():N}",
            new[] { (serviceName, 1, unitPrice, (Guid?)null, amountCollected) },
            discount: 0, paymentMethod: PaymentMethod.Cash);
        invoice.MarkAsPaid(PaymentMethod.Cash);
        SetPaymentDate(invoice, paymentDate);
        return invoice;
    }

    private static void SetPaymentDate(Invoice invoice, DateTimeOffset date)
        => typeof(Invoice).GetProperty(nameof(Invoice.PaymentDate))!.SetValue(invoice, date);

    private static void SetCreatedAt(Invoice invoice, DateTimeOffset date)
        => typeof(Invoice).GetProperty(nameof(Invoice.CreatedAt))!.SetValue(invoice, date);

    // ── GetSummaryAsync ──────────────────────────────────────────────────────

    [Test]
    public async Task GetSummaryAsync_PaidInvoiceInPeriod_CountsTowardCollected()
    {
        var (_, _, appt) = await SeedAppointmentAsync();
        var invoice = IssuePaidInvoice(appt.Id, "Trám răng", 500_000m, new DateTimeOffset(2026, 8, 15, 3, 0, 0, TimeSpan.Zero));
        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync();

        var result = await _sut.GetSummaryAsync(PeriodFrom, PeriodTo, CancellationToken.None);

        result.TotalCollected.Should().Be(500_000m);
        result.TotalUncollected.Should().Be(0m);
    }

    [Test]
    public async Task GetSummaryAsync_UnpaidInvoiceInPeriod_CountsTowardUncollected()
    {
        var (_, _, appt) = await SeedAppointmentAsync();
        var invoice = Invoice.Issue(
            appt.Id, $"INV-{Guid.NewGuid():N}",
            new[] { ("Nhổ răng khôn", 1, 800_000m, (Guid?)null, (decimal?)null) },
            discount: 0, paymentMethod: PaymentMethod.Cash);
        SetCreatedAt(invoice, new DateTimeOffset(2026, 8, 12, 3, 0, 0, TimeSpan.Zero));
        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync();

        var result = await _sut.GetSummaryAsync(PeriodFrom, PeriodTo, CancellationToken.None);

        result.TotalUncollected.Should().Be(800_000m);
        result.TotalCollected.Should().Be(0m);
    }

    [Test]
    public async Task GetSummaryAsync_RefundedInvoice_CountsTowardRefunded()
    {
        var (_, _, appt) = await SeedAppointmentAsync();
        var invoice = IssuePaidInvoice(appt.Id, "Tẩy trắng răng", 1_200_000m, new DateTimeOffset(2026, 8, 5, 3, 0, 0, TimeSpan.Zero));
        invoice.Refund();
        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync();

        var result = await _sut.GetSummaryAsync(PeriodFrom, PeriodTo, CancellationToken.None);

        result.TotalRefunded.Should().Be(1_200_000m);
        result.TotalCollected.Should().Be(0m); // đã Refunded thì không còn tính là Paid
    }

    /// <summary>
    /// Hóa đơn đặt cọc (Paid, DepositAmount &lt; TotalAmount) mà CHƯA có hóa đơn con thu nốt
    /// phải tính phần còn nợ vào Chưa thu.
    /// </summary>
    [Test]
    public async Task GetSummaryAsync_UnsettledDeposit_NoChildYet_CountsRemainingAsUncollected()
    {
        var (_, _, appt) = await SeedAppointmentAsync();
        var invoice = IssuePaidInvoice(appt.Id, "Niềng răng", 10_000_000m, new DateTimeOffset(2026, 8, 3, 3, 0, 0, TimeSpan.Zero), amountCollected: 3_000_000m);
        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync();

        var result = await _sut.GetSummaryAsync(PeriodFrom, PeriodTo, CancellationToken.None);

        result.TotalCollected.Should().Be(3_000_000m);
        result.TotalUncollected.Should().Be(7_000_000m);
    }

    /// <summary>
    /// Một khi đã tạo hóa đơn con thu phần còn lại (dù chưa trả), phần còn nợ của hóa đơn cọc gốc
    /// KHÔNG được đếm thêm lần nữa — tránh đếm trùng với chính hóa đơn con (Unpaid) đó.
    /// </summary>
    [Test]
    public async Task GetSummaryAsync_DepositWithUnpaidChild_DoesNotDoubleCountRemaining()
    {
        var (_, _, appt) = await SeedAppointmentAsync();
        var parent = IssuePaidInvoice(appt.Id, "Niềng răng", 10_000_000m, new DateTimeOffset(2026, 8, 3, 3, 0, 0, TimeSpan.Zero), amountCollected: 3_000_000m);
        _db.Invoices.Add(parent);
        await _db.SaveChangesAsync();

        var child = Invoice.IssueRemaining(appt.Id, $"INV-{Guid.NewGuid():N}", parent.Id, "Thu phần còn lại", 7_000_000m, PaymentMethod.Cash);
        SetCreatedAt(child, new DateTimeOffset(2026, 8, 20, 3, 0, 0, TimeSpan.Zero));
        _db.Invoices.Add(child);
        await _db.SaveChangesAsync();

        var result = await _sut.GetSummaryAsync(PeriodFrom, PeriodTo, CancellationToken.None);

        // Tổng chưa thu vẫn đúng 7 triệu — không phải 14 triệu (7tr từ child Unpaid + 7tr đếm trùng từ parent).
        result.TotalUncollected.Should().Be(7_000_000m);
    }

    [Test]
    public async Task GetSummaryAsync_TotalBilled_ExcludesChildRemainingInvoiceAmount()
    {
        var (_, _, appt) = await SeedAppointmentAsync();
        var parent = IssuePaidInvoice(appt.Id, "Niềng răng", 10_000_000m, new DateTimeOffset(2026, 8, 3, 3, 0, 0, TimeSpan.Zero), amountCollected: 3_000_000m);
        SetCreatedAt(parent, new DateTimeOffset(2026, 8, 3, 3, 0, 0, TimeSpan.Zero));
        _db.Invoices.Add(parent);
        await _db.SaveChangesAsync();

        var child = Invoice.IssueRemaining(appt.Id, $"INV-{Guid.NewGuid():N}", parent.Id, "Thu phần còn lại", 7_000_000m, PaymentMethod.Cash);
        SetCreatedAt(child, new DateTimeOffset(2026, 8, 20, 3, 0, 0, TimeSpan.Zero));
        _db.Invoices.Add(child);
        await _db.SaveChangesAsync();

        var result = await _sut.GetSummaryAsync(PeriodFrom, PeriodTo, CancellationToken.None);

        // Tổng doanh thu = đúng 10 triệu (giá trị điều trị thật) — không phải 17 triệu.
        result.TotalBilled.Should().Be(10_000_000m);
    }

    // ── GetTransactionsPagedAsync ────────────────────────────────────────────

    [Test]
    public async Task GetTransactionsPagedAsync_FilterByDentistId_ReturnsOnlyMatching()
    {
        var (_, dentistA, apptA) = await SeedAppointmentAsync();
        var (_, _, apptB) = await SeedAppointmentAsync();
        var invA = IssuePaidInvoice(apptA.Id, "Trám răng", 500_000m, new DateTimeOffset(2026, 8, 10, 3, 0, 0, TimeSpan.Zero));
        SetCreatedAt(invA, new DateTimeOffset(2026, 8, 10, 3, 0, 0, TimeSpan.Zero));
        var invB = IssuePaidInvoice(apptB.Id, "Trám răng", 500_000m, new DateTimeOffset(2026, 8, 11, 3, 0, 0, TimeSpan.Zero));
        SetCreatedAt(invB, new DateTimeOffset(2026, 8, 11, 3, 0, 0, TimeSpan.Zero));
        _db.Invoices.AddRange(invA, invB);
        await _db.SaveChangesAsync();

        var result = await _sut.GetTransactionsPagedAsync(
            new RevenueTransactionsFilter(PeriodFrom, PeriodTo, dentistA.Id, null, null, null, null, 1, 20, null, null),
            CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Single().DentistId.Should().Be(dentistA.Id);
    }

    [Test]
    public async Task GetTransactionsPagedAsync_MultiItemInvoice_SummarizesServiceNames()
    {
        var (_, _, appt) = await SeedAppointmentAsync();
        var invoice = Invoice.Issue(
            appt.Id, $"INV-{Guid.NewGuid():N}",
            new[]
            {
                ("Trám răng", 1, 500_000m, (Guid?)null, (decimal?)null),
                ("Cạo vôi răng", 1, 300_000m, (Guid?)null, (decimal?)null),
            },
            discount: 0, paymentMethod: PaymentMethod.Cash);
        SetCreatedAt(invoice, new DateTimeOffset(2026, 8, 10, 3, 0, 0, TimeSpan.Zero));
        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync();

        var result = await _sut.GetTransactionsPagedAsync(
            new RevenueTransactionsFilter(PeriodFrom, PeriodTo, null, null, null, null, null, 1, 20, null, null),
            CancellationToken.None);

        // DescribeServices sắp xếp tên theo alphabet (Ordinal) để ổn định — "Cạo..." < "Trám..." theo thứ tự này.
        result.Items.Single().ServiceSummary.Should().Be("Cạo vôi răng +1 dịch vụ khác");
    }

    [Test]
    public async Task GetTransactionsPagedAsync_FilterByStatus_ReturnsOnlyMatching()
    {
        var (_, _, appt) = await SeedAppointmentAsync();
        var paid = IssuePaidInvoice(appt.Id, "Trám răng", 500_000m, new DateTimeOffset(2026, 8, 10, 3, 0, 0, TimeSpan.Zero));
        SetCreatedAt(paid, new DateTimeOffset(2026, 8, 10, 3, 0, 0, TimeSpan.Zero));
        var unpaid = Invoice.Issue(
            appt.Id, $"INV-{Guid.NewGuid():N}",
            new[] { ("Nhổ răng khôn", 1, 800_000m, (Guid?)null, (decimal?)null) },
            discount: 0, paymentMethod: PaymentMethod.Cash);
        SetCreatedAt(unpaid, new DateTimeOffset(2026, 8, 11, 3, 0, 0, TimeSpan.Zero));
        _db.Invoices.AddRange(paid, unpaid);
        await _db.SaveChangesAsync();

        var result = await _sut.GetTransactionsPagedAsync(
            new RevenueTransactionsFilter(PeriodFrom, PeriodTo, null, null, "Paid", null, null, 1, 20, null, null),
            CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Single().Status.Should().Be("Paid");
    }

    // ── GetChartsAsync ───────────────────────────────────────────────────────

    [Test]
    public async Task GetChartsAsync_GroupsCollectedAmountByServiceAndDentist()
    {
        var (_, dentist, appt) = await SeedAppointmentAsync();
        var inv1 = IssuePaidInvoice(appt.Id, "Trám răng", 500_000m, new DateTimeOffset(2026, 8, 10, 3, 0, 0, TimeSpan.Zero));
        var inv2 = IssuePaidInvoice(appt.Id, "Trám răng", 500_000m, new DateTimeOffset(2026, 8, 12, 3, 0, 0, TimeSpan.Zero));
        _db.Invoices.AddRange(inv1, inv2);
        await _db.SaveChangesAsync();

        var result = await _sut.GetChartsAsync(PeriodFrom, PeriodTo, CancellationToken.None);

        result.ByService.Should().ContainSingle(s => s.ServiceName == "Trám răng" && s.Amount == 1_000_000m);
        result.ByDentist.Should().ContainSingle(d => d.DentistId == dentist.Id && d.Amount == 1_000_000m);
    }
}
