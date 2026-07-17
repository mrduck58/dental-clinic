using DentalClinic.API.Application.UseCases.Appointments;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Handlers;

[TestFixture]
public class DiagnosisHandlerTests
{
    private AppDbContext _db = null!;
    private DiagnosisHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _handler = new DiagnosisHandler(_db);
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
        appointmentId, "K02.1", "Sâu răng ngà", "Ghi chú", 75, 36.5m, 120, 80,
        null, null, "Răng số 6 hàm dưới", "Cần trám");

    /// <summary>Tạo chẩn đoán cho lịch hẹn không tồn tại phải báo lỗi thay vì tạo dữ liệu mồ côi.</summary>
    [Test]
    public async Task CreateAsync_AppointmentNotFound_ThrowsKeyNotFoundException()
    {
        Func<Task> act = () => _handler.CreateAsync(MakeCreateRequest(Guid.NewGuid()));

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

        Func<Task> act = () => _handler.CreateAsync(MakeCreateRequest(appointment.Id));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    /// <summary>Tạo chẩn đoán hợp lệ phải lưu vào DB và trả về đúng dữ liệu.</summary>
    [Test]
    public async Task CreateAsync_ValidRequest_SavesAndReturnsDto()
    {
        var appointment = await SeedInProgressAppointmentAsync();

        var result = await _handler.CreateAsync(MakeCreateRequest(appointment.Id));

        result.DiagnosisCode.Should().Be("K02.1");
        result.Description.Should().Be("Sâu răng ngà");
        (await _db.Diagnoses.CountAsync()).Should().Be(1);
    }

    /// <summary>Cập nhật chẩn đoán không tồn tại phải báo lỗi.</summary>
    [Test]
    public async Task UpdateAsync_DiagnosisNotFound_ThrowsKeyNotFoundException()
    {
        var request = new UpdateDiagnosisRequest(
            Guid.NewGuid(), "K02.2", "Mới", null, null, null, null, null, null, null, null, null);

        Func<Task> act = () => _handler.UpdateAsync(request);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    /// <summary>Cập nhật chẩn đoán hợp lệ phải ghi đè đúng các trường mới.</summary>
    [Test]
    public async Task UpdateAsync_ValidRequest_UpdatesFields()
    {
        var appointment = await SeedInProgressAppointmentAsync();
        var created = await _handler.CreateAsync(MakeCreateRequest(appointment.Id));

        var updateRequest = new UpdateDiagnosisRequest(
            created.Id, "K04.0", "Viêm tủy răng", "Ghi chú mới", null, null, null, null, null, null, null, "Kết luận mới");
        var result = await _handler.UpdateAsync(updateRequest);

        result.DiagnosisCode.Should().Be("K04.0");
        result.Description.Should().Be("Viêm tủy răng");
        result.Conclusion.Should().Be("Kết luận mới");
    }

    /// <summary>Xóa chẩn đoán không tồn tại phải báo lỗi.</summary>
    [Test]
    public async Task DeleteAsync_DiagnosisNotFound_ThrowsKeyNotFoundException()
    {
        Func<Task> act = () => _handler.DeleteAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    /// <summary>Xóa chẩn đoán tồn tại phải loại bỏ khỏi DB.</summary>
    [Test]
    public async Task DeleteAsync_DiagnosisExists_RemovesFromDb()
    {
        var appointment = await SeedInProgressAppointmentAsync();
        var created = await _handler.CreateAsync(MakeCreateRequest(appointment.Id));

        await _handler.DeleteAsync(created.Id);

        (await _db.Diagnoses.CountAsync()).Should().Be(0);
    }
}
