using DentalClinic.API.Application.UseCases.ClinicalRecords;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Infrastructure.Persistence;
using DentalClinic.API.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Handlers;

[TestFixture]
public class TreatmentSupplyUsageHandlerTests
{
    private AppDbContext _db = null!;
    private IActivityLogService _activityLogService = null!;
    private RecordTreatmentSupplyUsageHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _activityLogService = Substitute.For<IActivityLogService>();

        var treatmentPlanRepository = new TreatmentPlanRepository(_db);
        var appointmentRepository = new AppointmentRepository(_db);
        var procedureRepository = new TreatmentProcedureRepository(_db);
        var queryHelper = new TreatmentPlanQueryHelper(treatmentPlanRepository, appointmentRepository, procedureRepository);

        _handler = new RecordTreatmentSupplyUsageHandler(
            treatmentPlanRepository,
            new SupplyItemRepository(_db),
            new SupplyTransactionRepository(_db),
            new TreatmentSupplyUsageRepository(_db),
            queryHelper,
            _activityLogService);
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    private async Task<(Patient patient, DentistProfile dentist)> SeedPatientAndDentistAsync(string username)
    {
        var patientUser = User.Create($"{username}-p", $"{username}-p@test.com", "hash", UserRole.Patient);
        var dentistUser = User.Create($"{username}-d", $"{username}-d@test.com", "hash", UserRole.Dentist);
        _db.Users.AddRange(patientUser, dentistUser);
        var patient = Patient.Create(patientUser.Id, new DateOnly(1990, 1, 1), "Nam");
        var employee = Employee.Create(dentistUser.Id, $"DT-{Guid.NewGuid():N}");
        employee.User = dentistUser;
        var dentist = DentistProfile.Create(employee.Id, "Nha khoa tổng quát", "N/A", 5);
        dentist.Employee = employee;
        _db.Patients.Add(patient);
        _db.Employees.Add(employee);
        _db.DentistProfiles.Add(dentist);
        await _db.SaveChangesAsync();
        return (patient, dentist);
    }

    private async Task<(Appointment appointment, Service service)> SeedInProgressAppointmentAsync(Patient patient, DentistProfile dentist)
    {
        var service = Service.Create("Trám răng", 500_000m, 30, "Trám răng thẩm mỹ");
        _db.Services.Add(service);
        var appointment = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        appointment.StartTreatment();
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();
        return (appointment, service);
    }

    private async Task<TreatmentPlan> SeedActiveTreatmentPlanAsync(string username)
    {
        var (patient, dentist) = await SeedPatientAndDentistAsync(username);
        var (appointment, service) = await SeedInProgressAppointmentAsync(patient, dentist);
        var plan = TreatmentPlan.Create(patient.Id, dentist.Id, appointment.Id, service.Id, 500_000m, 1);
        _db.TreatmentPlans.Add(plan);
        await _db.SaveChangesAsync();
        return plan;
    }

    private async Task<SupplyItem> SeedSupplyItemAsync(string name, int quantity, decimal? price = 10_000m)
    {
        var item = SupplyItem.Create("VT-001", name, "Vật dụng", "Cái", quantity, 5, price: price);
        _db.SupplyItems.Add(item);
        await _db.SaveChangesAsync();
        return item;
    }

    [Test]
    public async Task RecordAsync_TreatmentPlanNotFound_ThrowsNotFoundException()
    {
        var item = await SeedSupplyItemAsync("Găng tay", 50);
        var request = new RecordTreatmentSupplyUsageRequest([new(item.Id, 1)]);

        Func<Task> act = () => _handler.Handle(new RecordTreatmentSupplyUsageCommand(Guid.NewGuid(), request), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task RecordAsync_EmptyItems_ThrowsValidationException()
    {
        var plan = await SeedActiveTreatmentPlanAsync("u1");
        var request = new RecordTreatmentSupplyUsageRequest([]);

        Func<Task> act = () => _handler.Handle(new RecordTreatmentSupplyUsageCommand(plan.Id, request), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task RecordAsync_DuplicateSupplyItem_ThrowsValidationException()
    {
        var plan = await SeedActiveTreatmentPlanAsync("u2");
        var item = await SeedSupplyItemAsync("Găng tay", 50);
        var request = new RecordTreatmentSupplyUsageRequest([new(item.Id, 1), new(item.Id, 2)]);

        Func<Task> act = () => _handler.Handle(new RecordTreatmentSupplyUsageCommand(plan.Id, request), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task RecordAsync_QuantityExceedsStock_ThrowsValidationException()
    {
        var plan = await SeedActiveTreatmentPlanAsync("u3");
        var item = await SeedSupplyItemAsync("Găng tay", 2);
        var request = new RecordTreatmentSupplyUsageRequest([new(item.Id, 5)]);

        Func<Task> act = () => _handler.Handle(new RecordTreatmentSupplyUsageCommand(plan.Id, request), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Ghi nhận hợp lệ phải trừ đúng số lượng tồn kho, tạo giao dịch "export", và lưu snapshot giá vốn.</summary>
    [Test]
    public async Task RecordAsync_ValidRequest_DeductsStockAndSnapshotsUnitCost()
    {
        var plan = await SeedActiveTreatmentPlanAsync("u4");
        var item = await SeedSupplyItemAsync("Chỉ khâu", 20, price: 15_000m);
        var request = new RecordTreatmentSupplyUsageRequest([new(item.Id, 3)]);

        var result = await _handler.Handle(new RecordTreatmentSupplyUsageCommand(plan.Id, request), CancellationToken.None);

        result.Should().ContainSingle(u => u.SupplyItemName == "Chỉ khâu" && u.Quantity == 3 && u.UnitCostAtUsage == 15_000m);
        (await _db.SupplyItems.FindAsync(item.Id))!.Quantity.Should().Be(17);
        (await _db.SupplyTransactions.CountAsync(t => t.SupplyItemId == item.Id && t.Type == "export")).Should().Be(1);
    }

    [Test]
    public async Task GetByTreatmentPlanAsync_ReturnsPreviouslyRecordedUsage()
    {
        var plan = await SeedActiveTreatmentPlanAsync("u5");
        var item = await SeedSupplyItemAsync("Bông gòn", 30);
        await _handler.Handle(
            new RecordTreatmentSupplyUsageCommand(plan.Id, new RecordTreatmentSupplyUsageRequest([new(item.Id, 2)])),
            CancellationToken.None);

        var result = await _handler.Handle(new GetTreatmentSupplyUsageQuery(plan.Id), CancellationToken.None);

        result.Should().ContainSingle(u => u.SupplyItemName == "Bông gòn" && u.Quantity == 2);
    }
}
