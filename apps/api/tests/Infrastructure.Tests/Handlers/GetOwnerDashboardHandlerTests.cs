using DentalClinic.API.Application.UseCases.OwnerDashboard;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Infrastructure.Persistence;
using DentalClinic.API.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Handlers;

[TestFixture]
public class GetOwnerDashboardHandlerTests
{
    private AppDbContext _db = null!;
    private GetOwnerDashboardHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _handler = new GetOwnerDashboardHandler(new OwnerDashboardQueryService(_db));
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    [Test]
    public async Task Handle_ShouldReturnCorrectOwnerDashboardData()
    {
        // Arrange: seed Patient, Feedback, Invoice, SupplyTransaction, DentistProfile
        var patientUser = User.Create("p1", "p1@test.com", "hash", UserRole.Patient, fullName: "Nguyễn Văn Bệnh Nhân");
        var dentistUser = User.Create("d1", "d1@test.com", "hash", UserRole.Dentist, fullName: "BS. Trần Văn Hùng");
        _db.Users.AddRange(patientUser, dentistUser);

        var employee = Employee.Create(dentistUser.Id, "EMP-001");
        employee.User = dentistUser;
        var dentist = DentistProfile.Create(employee.Id, "Chỉnh nha", "LIC-123", 5);
        dentist.Employee = employee;
        _db.DentistProfiles.Add(dentist);

        var patient = Patient.Create(patientUser.Id, new DateOnly(1995, 5, 5), "Nam");
        _db.Patients.Add(patient);

        var items = new List<(string Name, int Quantity, decimal UnitPrice, Guid? TreatmentPlanId, decimal? AmountCollected)>
        {
            ("Tẩy trắng răng", 1, 5000000m, null, 5000000m)
        };
        var invoice = Invoice.Issue(Guid.NewGuid(), "INV-001", items, 0, PaymentMethod.Cash);
        invoice.MarkAsPaid(PaymentMethod.Cash);
        _db.Invoices.Add(invoice);

        var supply = SupplyItem.Create("V01", "Trụ Implant", "Vật liệu", "Cái", 10, 2);
        _db.SupplyItems.Add(supply);
        var tx = SupplyTransaction.Create(supply.Id, "import", 5, "Nhập kho thử nghiệm", "Admin", 1000000);
        _db.SupplyTransactions.Add(tx);

        var feedback = DentalClinic.API.Domain.Entities.Feedback.Create("Nguyễn Văn Bệnh Nhân", 5, "Rất hài lòng với phòng khám", patient.Id);
        _db.Feedbacks.Add(feedback);

        await _db.SaveChangesAsync();

        // Act
        var result = await _handler.Handle(new GetOwnerDashboardQuery(), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalRevenue.Should().Be(5000000);
        result.TotalExpense.Should().Be(5000000); // 5 * 1,000,000
        result.NewPatientsCount.Should().Be(1);
        result.WeeklyTrend.Should().HaveCount(7);
        result.RatingStats.TotalReviews.Should().Be(1);
        result.RatingStats.AverageRating.Should().Be(5.0);
        result.OutstandingEmployees.Should().NotBeEmpty();
    }
}
