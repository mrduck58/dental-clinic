using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Infrastructure.Persistence;
using DentalClinic.API.Infrastructure.Persistence.Repositories;
using DentalClinic.API.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Services;

[TestFixture]
public class CommissionQueryServiceTests
{
    private AppDbContext _db = null!;
    private CommissionQueryService _sut = null!;
    private ICommissionRuleRepository _ruleRepo = null!;

    private static readonly DateOnly PeriodFrom = new(2026, 8, 1);
    private static readonly DateOnly PeriodTo = new(2026, 8, 31);

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _ruleRepo = new CommissionRuleRepository(_db);
        _sut = new CommissionQueryService(_db, _ruleRepo);
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    private async Task<(DentistProfile dentistA, Appointment apptA, DentistProfile dentistB, Appointment apptB)> SeedTwoDentistsAsync()
    {
        async Task<(DentistProfile, Appointment)> SeedOne(string tag)
        {
            var patientUser = User.Create($"p-{tag}", $"p-{tag}-{Guid.NewGuid()}@test.com", "hash", UserRole.Patient);
            var dentistUser = User.Create($"d-{tag}", $"d-{tag}-{Guid.NewGuid()}@test.com", "hash", UserRole.Dentist, fullName: $"Bác sĩ {tag.ToUpperInvariant()}");
            _db.Users.AddRange(patientUser, dentistUser);
            var patient = Patient.Create(patientUser.Id, new DateOnly(1990, 1, 1), "Nam");
            var employee = Employee.Create(dentistUser.Id, $"DT-{Guid.NewGuid():N}");
            employee.User = dentistUser;
            var dentist = DentistProfile.Create(employee.Id, "Nha khoa tổng quát", "N/A", 5);
            dentist.Employee = employee;
            _db.Patients.Add(patient);
            _db.Employees.Add(employee);
            _db.DentistProfiles.Add(dentist);
            var appt = Appointment.Create(patient.Id, dentist.Id, new DateTimeOffset(2026, 8, 10, 8, 0, 0, TimeSpan.Zero));
            _db.Appointments.Add(appt);
            await _db.SaveChangesAsync();
            return (dentist, appt);
        }

        var (dentistA, apptA) = await SeedOne("a");
        var (dentistB, apptB) = await SeedOne("b");
        return (dentistA, apptA, dentistB, apptB);
    }

    private Invoice IssuePaid(Guid appointmentId, string serviceName, decimal unitPrice, DateTimeOffset paymentDate)
    {
        var invoice = Invoice.Issue(
            appointmentId, $"INV-{Guid.NewGuid():N}",
            new[] { (serviceName, 1, unitPrice, (Guid?)null, (decimal?)null) },
            discount: 0, paymentMethod: PaymentMethod.Cash);
        invoice.MarkAsPaid(PaymentMethod.Cash);
        typeof(Invoice).GetProperty(nameof(Invoice.PaymentDate))!.SetValue(invoice, paymentDate);
        return invoice;
    }

    [Test]
    public async Task GetRulesWithCommission_NoScopeFilter_SumsAllPaidRevenue()
    {
        var (_, apptA, _, apptB) = await SeedTwoDentistsAsync();
        var inv1 = IssuePaid(apptA.Id, "Trám răng", 1_000_000m, new DateTimeOffset(2026, 8, 10, 3, 0, 0, TimeSpan.Zero));
        var inv2 = IssuePaid(apptB.Id, "Trám răng", 1_000_000m, new DateTimeOffset(2026, 8, 11, 3, 0, 0, TimeSpan.Zero));
        _db.Invoices.AddRange(inv1, inv2);
        await _db.SaveChangesAsync();

        var rule = CommissionRule.Create(null, null, 10m, new DateOnly(2026, 1, 1), null, null);
        _db.CommissionRules.Add(rule);
        await _db.SaveChangesAsync();

        var result = await _sut.GetRulesWithCommissionAsync(PeriodFrom, PeriodTo, CancellationToken.None);

        var dto = result.Items.Single();
        dto.RevenueBasis.Should().Be(2_000_000m);
        dto.CommissionAmount.Should().Be(200_000m); // 10% of 2tr
    }

    [Test]
    public async Task GetRulesWithCommission_DentistScoped_OnlyCountsThatDentistRevenue()
    {
        var (dentistA, apptA, _, apptB) = await SeedTwoDentistsAsync();
        var inv1 = IssuePaid(apptA.Id, "Trám răng", 1_000_000m, new DateTimeOffset(2026, 8, 10, 3, 0, 0, TimeSpan.Zero));
        var inv2 = IssuePaid(apptB.Id, "Trám răng", 1_000_000m, new DateTimeOffset(2026, 8, 11, 3, 0, 0, TimeSpan.Zero));
        _db.Invoices.AddRange(inv1, inv2);
        await _db.SaveChangesAsync();

        var rule = CommissionRule.Create(dentistA.Id, null, 20m, new DateOnly(2026, 1, 1), null, null);
        _db.CommissionRules.Add(rule);
        await _db.SaveChangesAsync();

        var result = await _sut.GetRulesWithCommissionAsync(PeriodFrom, PeriodTo, CancellationToken.None);

        var dto = result.Items.Single();
        dto.RevenueBasis.Should().Be(1_000_000m); // chỉ dentistA
        dto.CommissionAmount.Should().Be(200_000m); // 20% of 1tr
        dto.DentistName.Should().NotBeNullOrEmpty();
    }

    [Test]
    public async Task GetRulesWithCommission_ServiceScoped_UsesItemAmountCollectedOnly()
    {
        var (_, apptA, _, _) = await SeedTwoDentistsAsync();
        var invoice = Invoice.Issue(
            apptA.Id, $"INV-{Guid.NewGuid():N}",
            new[]
            {
                ("Trám răng", 1, 500_000m, (Guid?)null, (decimal?)null),
                ("Cạo vôi răng", 1, 300_000m, (Guid?)null, (decimal?)null),
            },
            discount: 0, paymentMethod: PaymentMethod.Cash);
        invoice.MarkAsPaid(PaymentMethod.Cash);
        typeof(Invoice).GetProperty(nameof(Invoice.PaymentDate))!.SetValue(invoice, new DateTimeOffset(2026, 8, 10, 3, 0, 0, TimeSpan.Zero));
        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync();

        var rule = CommissionRule.Create(null, "Trám răng", 10m, new DateOnly(2026, 1, 1), null, null);
        _db.CommissionRules.Add(rule);
        await _db.SaveChangesAsync();

        var result = await _sut.GetRulesWithCommissionAsync(PeriodFrom, PeriodTo, CancellationToken.None);

        var dto = result.Items.Single();
        dto.RevenueBasis.Should().Be(500_000m); // chỉ dòng Trám răng, không tính Cạo vôi răng
    }

    [Test]
    public async Task GetRulesWithCommission_RuleNotYetEffectiveInPeriod_ReturnsZeroBasis()
    {
        var (_, apptA, _, _) = await SeedTwoDentistsAsync();
        var invoice = IssuePaid(apptA.Id, "Trám răng", 1_000_000m, new DateTimeOffset(2026, 8, 10, 3, 0, 0, TimeSpan.Zero));
        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync();

        // Quy tắc chỉ có hiệu lực từ tháng 9 — không giao với kỳ tháng 8 đang xét.
        var rule = CommissionRule.Create(null, null, 10m, new DateOnly(2026, 9, 1), null, null);
        _db.CommissionRules.Add(rule);
        await _db.SaveChangesAsync();

        var result = await _sut.GetRulesWithCommissionAsync(PeriodFrom, PeriodTo, CancellationToken.None);

        var dto = result.Items.Single();
        dto.RevenueBasis.Should().Be(0m);
        dto.CommissionAmount.Should().Be(0m);
    }
}
