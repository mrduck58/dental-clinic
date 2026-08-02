using DentalClinic.API.Application.UseCases.Inventory;
using DentalClinic.API.Domain.Entities;
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
        _handler = new CreateMaterialRequestHandler(_db, new MaterialRequestRepository(_db));
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    private async Task<Appointment> SeedAppointmentAsync(bool withService = true)
    {
        var patientUser = User.Create("mr-p", $"mr-p-{Guid.NewGuid()}@test.com", "hash", "Patient");
        var dentistUser = User.Create("mr-d", $"mr-d-{Guid.NewGuid()}@test.com", "hash", "Dentist");
        _db.Users.AddRange(patientUser, dentistUser);
        var patient = Patient.Create(patientUser.Id, new DateOnly(1990, 1, 1), "Nam");
        var dentist = Dentist.Create(dentistUser.Id, "Nha khoa tổng quát", 5);
        _db.Patients.Add(patient);
        _db.Dentists.Add(dentist);

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

    /// <summary>Nội dung yêu cầu trống phải bị từ chối.</summary>
    [Test]
    public async Task HandleAsync_EmptyContent_ThrowsValidationException()
    {
        var appointment = await SeedAppointmentAsync();

        Func<Task> act = () => _handler.Handle(
            new CreateMaterialRequestRequest(appointment.Id, "   "), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Lịch hẹn không tồn tại phải báo lỗi NotFoundException.</summary>
    [Test]
    public async Task HandleAsync_AppointmentNotFound_ThrowsNotFoundException()
    {
        Func<Task> act = () => _handler.Handle(
            new CreateMaterialRequestRequest(Guid.NewGuid(), "Cần thêm vật tư"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>Yêu cầu hợp lệ cho lịch hẹn có dịch vụ phải tạo và lưu đúng yêu cầu vật tư với tên khóa học
    /// lấy từ tên dịch vụ, nội dung được trim khoảng trắng thừa.</summary>
    [Test]
    public async Task HandleAsync_ValidRequestWithService_CreatesAndPersistsMaterialRequest()
    {
        var appointment = await SeedAppointmentAsync(withService: true);
        var appt = await _db.Appointments.Include(a => a.Patient).ThenInclude(p => p.User)
            .Include(a => a.Dentist).ThenInclude(d => d.User)
            .Include(a => a.Service)
            .FirstAsync(a => a.Id == appointment.Id);

        var result = await _handler.Handle(
            new CreateMaterialRequestRequest(appointment.Id, "  Cần thêm trụ implant  "), CancellationToken.None);

        result.CourseName.Should().Be("Trồng Implant");
        result.PatientName.Should().Be(appt.Patient.FullName);
        result.DentistName.Should().Be(appt.Dentist.FullName);
        result.Content.Should().Be("Cần thêm trụ implant");
        result.Status.Should().Be(MaterialRequestStatus.Pending.ToString());

        var persisted = await _db.MaterialRequests.SingleAsync(m => m.Id == result.Id);
        persisted.PatientName.Should().Be(appt.Patient.FullName);
        persisted.Content.Should().Be("Cần thêm trụ implant");
    }

    /// <summary>Lịch hẹn không gắn dịch vụ (khám tổng quát) phải dùng tên mặc định "Khám tổng quát".</summary>
    [Test]
    public async Task HandleAsync_AppointmentWithoutService_UsesDefaultCourseName()
    {
        var appointment = await SeedAppointmentAsync(withService: false);

        var result = await _handler.Handle(
            new CreateMaterialRequestRequest(appointment.Id, "Cần bông gòn"), CancellationToken.None);

        result.CourseName.Should().Be("Khám tổng quát");
    }
}
