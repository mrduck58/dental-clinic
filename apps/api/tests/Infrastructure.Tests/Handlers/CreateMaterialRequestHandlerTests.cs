using DentalClinic.API.Application.UseCases.Inventory;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Infrastructure.Persistence;
using DentalClinic.API.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Handlers;

[TestFixture]
public class CreateMaterialRequestHandlerTests
{
    private AppDbContext _db = null!;
    private CreateMaterialRequestHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _handler = new CreateMaterialRequestHandler(new AppointmentSummaryReader(_db), new MaterialRequestRepository(_db));
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    private async Task<Appointment> SeedAppointmentAsync(bool withService = true)
    {
        var patientUser = User.Create("mr-p", $"mr-p-{Guid.NewGuid()}@test.com", "hash", UserRole.Patient);
        var dentistUser = User.Create("mr-d", $"mr-d-{Guid.NewGuid()}@test.com", "hash", UserRole.Dentist);
        _db.Users.AddRange(patientUser, dentistUser);
        var patient = Patient.Create(patientUser.Id, new DateOnly(1990, 1, 1), "Nam");
        var employee = Employee.Create(dentistUser.Id, $"DT-{Guid.NewGuid():N}");
        employee.User = dentistUser;
        var dentist = DentistProfile.Create(employee.Id, "Nha khoa tổng quát", "N/A", 5);
        dentist.Employee = employee;
        _db.Patients.Add(patient);
        _db.Employees.Add(employee);
        _db.DentistProfiles.Add(dentist);

        Guid? serviceId = null;
        if (withService)
        {
            var service = Service.Create("Trồng Implant", 15_000_000m, 90, "Cấy ghép implant");
            _db.Services.Add(service);
            serviceId = service.Id;
        }

        var appointment = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow, serviceId: serviceId);
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();
        return appointment;
    }

    /// <summary>Danh sách vật tư trống phải bị từ chối.</summary>
    [Test]
    public async Task HandleAsync_EmptyItems_ThrowsValidationException()
    {
        var appointment = await SeedAppointmentAsync();

        Func<Task> act = () => _handler.Handle(
            new CreateMaterialRequestRequest(appointment.Id, []), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Một item có tên trống phải bị từ chối.</summary>
    [Test]
    public async Task HandleAsync_ItemWithBlankName_ThrowsValidationException()
    {
        var appointment = await SeedAppointmentAsync();

        Func<Task> act = () => _handler.Handle(
            new CreateMaterialRequestRequest(appointment.Id, [new MaterialRequestItemInput(" ", 2, "Cái")]),
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Một item có số lượng &lt;= 0 phải bị từ chối.</summary>
    [Test]
    public async Task HandleAsync_ItemWithZeroQuantity_ThrowsValidationException()
    {
        var appointment = await SeedAppointmentAsync();

        Func<Task> act = () => _handler.Handle(
            new CreateMaterialRequestRequest(appointment.Id, [new MaterialRequestItemInput("Bông gòn", 0, "Gói")]),
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Một item có đơn vị không nằm trong danh sách cho phép phải bị từ chối.</summary>
    [Test]
    public async Task HandleAsync_ItemWithInvalidUnit_ThrowsValidationException()
    {
        var appointment = await SeedAppointmentAsync();

        Func<Task> act = () => _handler.Handle(
            new CreateMaterialRequestRequest(appointment.Id, [new MaterialRequestItemInput("Bông gòn", 2, "Kilogram")]),
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Lịch hẹn không tồn tại phải báo lỗi NotFoundException.</summary>
    [Test]
    public async Task HandleAsync_AppointmentNotFound_ThrowsNotFoundException()
    {
        Func<Task> act = () => _handler.Handle(
            new CreateMaterialRequestRequest(Guid.NewGuid(), [new MaterialRequestItemInput("Bông gòn", 2, "Gói")]),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>Yêu cầu hợp lệ nhiều vật tư cho lịch hẹn có dịch vụ phải tạo và lưu đúng yêu cầu vật tư với tên
    /// khóa học lấy từ tên dịch vụ, tên từng vật tư được trim khoảng trắng thừa.</summary>
    [Test]
    public async Task HandleAsync_ValidRequestWithService_CreatesAndPersistsMaterialRequest()
    {
        var appointment = await SeedAppointmentAsync(withService: true);
        var appt = await _db.Appointments.Include(a => a.Patient).ThenInclude(p => p.User)
            .Include(a => a.Dentist).ThenInclude(d => d.Employee).ThenInclude(e => e.User)
            .Include(a => a.Service)
            .FirstAsync(a => a.Id == appointment.Id);

        var result = await _handler.Handle(
            new CreateMaterialRequestRequest(appointment.Id, [
                new MaterialRequestItemInput("  Trụ implant  ", 2, "Cái"),
                new MaterialRequestItemInput("Chỉ khâu 4/0", 1, "Cuộn"),
            ]),
            CancellationToken.None);

        result.CourseName.Should().Be("Trồng Implant");
        result.PatientName.Should().Be(appt.Patient.FullName);
        result.DentistName.Should().Be(appt.Dentist.FullName);
        result.Status.Should().Be(MaterialRequestStatus.Pending.ToString());
        result.Items.Should().HaveCount(2);
        result.Items[0].ItemName.Should().Be("Trụ implant");
        result.Items[0].Quantity.Should().Be(2);
        result.Items[0].Unit.Should().Be("Cái");

        var persisted = await _db.MaterialRequests.Include(m => m.Items).SingleAsync(m => m.Id == result.Id);
        persisted.PatientName.Should().Be(appt.Patient.FullName);
        persisted.Items.Should().HaveCount(2);
        persisted.Items.Should().Contain(i => i.ItemName == "Trụ implant" && i.Quantity == 2 && i.Unit == "Cái");
    }

    /// <summary>Lịch hẹn không gắn dịch vụ (khám tổng quát) phải dùng tên mặc định "Khám tổng quát".</summary>
    [Test]
    public async Task HandleAsync_AppointmentWithoutService_UsesDefaultCourseName()
    {
        var appointment = await SeedAppointmentAsync(withService: false);

        var result = await _handler.Handle(
            new CreateMaterialRequestRequest(appointment.Id, [new MaterialRequestItemInput("Bông gòn", 3, "Gói")]),
            CancellationToken.None);

        result.CourseName.Should().Be("Khám tổng quát");
    }
}
