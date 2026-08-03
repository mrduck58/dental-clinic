using DentalClinic.API.Application.DTOs.ClinicalRecords;
using DentalClinic.API.Application.UseCases.ClinicalRecords;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Infrastructure.Persistence;
using DentalClinic.API.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Handlers;

[TestFixture]
public class DiagnosisHandlerTests
{
    private AppDbContext _db = null!;
    // God-handler DiagnosisHandler (3 method) đã tách thành 3 handler MediatR.
    private CreateDiagnosisHandler _create = null!;
    private UpdateDiagnosisHandler _update = null!;
    private DeleteDiagnosisHandler _delete = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        var appointmentRepository = new AppointmentRepository(_db);
        var diagnosisRepository = new DiagnosisRepository(_db);
        _create = new CreateDiagnosisHandler(appointmentRepository, diagnosisRepository);
        _update = new UpdateDiagnosisHandler(diagnosisRepository);
        _delete = new DeleteDiagnosisHandler(diagnosisRepository);
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    private async Task<Appointment> SeedInProgressAppointmentAsync()
    {
        var dentistUser = User.Create("dg1", $"dg1-{Guid.NewGuid()}@test.com", "hash", "Dentist");
        _db.Users.Add(dentistUser);
        var dentist = Dentist.Create(dentistUser.Id, "Nha khoa tổng quát", 5);
        var patient = Patient.Create(Guid.Empty, new DateOnly(1990, 1, 1), "Nam");
        _db.Dentists.Add(dentist);
        _db.Patients.Add(patient);
        var appointment = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        appointment.StartTreatment();
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();
        return appointment;
    }

    private static CreateDiagnosisRequest MakeCreateRequest(Guid appointmentId) => new(
        appointmentId, "Sâu răng ngà", null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, "Cần trám");

    /// <summary>Tạo chẩn đoán cho lịch hẹn không tồn tại phải báo lỗi thay vì tạo dữ liệu mồ côi.</summary>
    [Test]
    public async Task CreateAsync_AppointmentNotFound_ThrowsKeyNotFoundException()
    {
        Func<Task> act = () => _create.Handle(MakeCreateRequest(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    /// <summary>Chỉ được thêm chẩn đoán khi buổi hẹn đang trong trạng thái đang khám (InProgress).</summary>
    [Test]
    public async Task CreateAsync_AppointmentNotInProgress_ThrowsInvalidOperationException()
    {
        var dentistUser = User.Create("dg2", $"dg2-{Guid.NewGuid()}@test.com", "hash", "Dentist");
        _db.Users.Add(dentistUser);
        var dentist = Dentist.Create(dentistUser.Id, "Nha khoa tổng quát", 5);
        var patient = Patient.Create(Guid.Empty, new DateOnly(1990, 1, 1), "Nam");
        _db.Dentists.Add(dentist);
        _db.Patients.Add(patient);
        var appointment = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        appointment.Confirm();
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();

        Func<Task> act = () => _create.Handle(MakeCreateRequest(appointment.Id), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    /// <summary>Tạo chẩn đoán hợp lệ phải lưu vào DB và trả về đúng dữ liệu.</summary>
    [Test]
    public async Task CreateAsync_ValidRequest_SavesAndReturnsDto()
    {
        var appointment = await SeedInProgressAppointmentAsync();

        var result = await _create.Handle(MakeCreateRequest(appointment.Id), CancellationToken.None);

        result.Description.Should().Be("Sâu răng ngà");
        (await _db.Diagnoses.CountAsync()).Should().Be(1);
    }

    /// <summary>Cập nhật chẩn đoán không tồn tại phải báo lỗi.</summary>
    [Test]
    public async Task UpdateAsync_DiagnosisNotFound_ThrowsKeyNotFoundException()
    {
        var request = new UpdateDiagnosisRequest(
            Guid.NewGuid(), "Mới", null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null);

        Func<Task> act = () => _update.Handle(request, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    /// <summary>Cập nhật chẩn đoán hợp lệ phải ghi đè đúng các trường mới.</summary>
    [Test]
    public async Task UpdateAsync_ValidRequest_UpdatesFields()
    {
        var appointment = await SeedInProgressAppointmentAsync();
        var created = await _create.Handle(MakeCreateRequest(appointment.Id), CancellationToken.None);

        var updateRequest = new UpdateDiagnosisRequest(
            created.Id, "Viêm tủy răng", null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, "Kết luận mới");
        var result = await _update.Handle(updateRequest, CancellationToken.None);
        result.Description.Should().Be("Viêm tủy răng");
        result.Conclusion.Should().Be("Kết luận mới");
    }

    /// <summary>Xóa chẩn đoán không tồn tại phải báo lỗi.</summary>
    [Test]
    public async Task DeleteAsync_DiagnosisNotFound_ThrowsKeyNotFoundException()
    {
        Func<Task> act = () => _delete.Handle(new DeleteDiagnosisCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    /// <summary>Xóa chẩn đoán tồn tại phải loại bỏ khỏi DB.</summary>
    [Test]
    public async Task DeleteAsync_DiagnosisExists_RemovesFromDb()
    {
        var appointment = await SeedInProgressAppointmentAsync();
        var created = await _create.Handle(MakeCreateRequest(appointment.Id), CancellationToken.None);

        await _delete.Handle(new DeleteDiagnosisCommand(created.Id), CancellationToken.None);

        (await _db.Diagnoses.CountAsync()).Should().Be(0);
    }
}
