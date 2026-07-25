using DentalClinic.API.Application.UseCases.Appointments;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Handlers;

[TestFixture]
public class FollowUpReminderHandlerTests
{
    private AppDbContext _db = null!;
    private FollowUpReminderHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _handler = new FollowUpReminderHandler(_db);
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    private async Task<(Patient patient, Dentist dentist, Service service)> SeedBaseDataAsync()
    {
        var dentistUser = User.Create("fu1", $"fu1-{Guid.NewGuid()}@test.com", "hash", "Dentist");
        _db.Users.Add(dentistUser);
        var dentist = Dentist.Create(dentistUser.Id, "Nha khoa tổng quát", 5);
        var patient = Patient.Create(Guid.Empty, new DateOnly(1990, 1, 1), "Nam");
        var service = Service.Create("Niềng răng", 20_000_000m, 60, "Chỉnh nha");
        _db.Dentists.Add(dentist);
        _db.Patients.Add(patient);
        _db.Services.Add(service);
        await _db.SaveChangesAsync();
        return (patient, dentist, service);
    }

    /// <summary>Đặt nhắc tái khám cho lịch hẹn không tồn tại phải báo lỗi NotFoundException.</summary>
    [Test]
    public async Task SetAsync_AppointmentNotFound_ThrowsNotFoundException()
    {
        var request = new SetFollowUpReminderRequest(DateOnly.FromDateTime(DateTime.Today.AddDays(7)), null);

        Func<Task> act = () => _handler.SetAsync(Guid.NewGuid(), request);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>Chỉ được hẹn tái khám khi buổi hẹn đang trong trạng thái đang khám (InProgress).</summary>
    [Test]
    public async Task SetAsync_AppointmentNotInProgress_ThrowsValidationException()
    {
        var (patient, dentist, _) = await SeedBaseDataAsync();
        var appointment = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        appointment.Confirm();
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();

        var request = new SetFollowUpReminderRequest(DateOnly.FromDateTime(DateTime.Today.AddDays(7)), null);
        Func<Task> act = () => _handler.SetAsync(appointment.Id, request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Ngày tái khám phải sau ngày hôm nay, không được chọn hôm nay hoặc quá khứ.</summary>
    [Test]
    public async Task SetAsync_DateNotInFuture_ThrowsValidationException()
    {
        var (patient, dentist, _) = await SeedBaseDataAsync();
        var appointment = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        appointment.StartTreatment();
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();

        var request = new SetFollowUpReminderRequest(DateOnly.FromDateTime(DateTime.Today), null);
        Func<Task> act = () => _handler.SetAsync(appointment.Id, request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Đặt nhắc tái khám hợp lệ phải lưu ngày và ghi chú vào lịch hẹn.</summary>
    [Test]
    public async Task SetAsync_ValidRequest_SetsReminderOnAppointment()
    {
        var (patient, dentist, _) = await SeedBaseDataAsync();
        var appointment = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        appointment.StartTreatment();
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();

        var followUpDate = DateOnly.FromDateTime(DateTime.Today.AddDays(14));
        var result = await _handler.SetAsync(appointment.Id, new SetFollowUpReminderRequest(followUpDate, "  Khám lại răng số 6  "));

        result.FollowUpDate.Should().Be(followUpDate);
        result.FollowUpNote.Should().Be("Khám lại răng số 6");
    }

    /// <summary>Xóa nhắc tái khám cho lịch hẹn không tồn tại phải báo lỗi.</summary>
    [Test]
    public async Task ClearAsync_AppointmentNotFound_ThrowsNotFoundException()
    {
        Func<Task> act = () => _handler.ClearAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>Xóa nhắc tái khám hợp lệ phải đặt lại ngày/ghi chú về null.</summary>
    [Test]
    public async Task ClearAsync_ValidRequest_ClearsReminder()
    {
        var (patient, dentist, _) = await SeedBaseDataAsync();
        var appointment = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        appointment.StartTreatment();
        appointment.SetFollowUpReminder(DateOnly.FromDateTime(DateTime.Today.AddDays(10)), "Note cũ");
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();

        var result = await _handler.ClearAsync(appointment.Id);

        result.FollowUpDate.Should().BeNull();
        result.FollowUpNote.Should().BeNull();
    }

    /// <summary>Không có liệu trình nào đang thực hiện thì danh sách chờ tái khám phải rỗng.</summary>
    [Test]
    public async Task GetDueAsync_NoActiveTreatmentPlans_ReturnsEmptyList()
    {
        var result = await _handler.GetDueAsync();

        result.Should().BeEmpty();
    }

    /// <summary>Bệnh nhân đã hoàn thành buổi hẹn nhưng còn liệu trình InProgress thuộc chuỗi đó
    /// phải xuất hiện trong danh sách chờ tái khám.</summary>
    [Test]
    public async Task GetDueAsync_PatientHasActivePlanAndCompletedVisit_ReturnsInDueList()
    {
        var (patient, dentist, service) = await SeedBaseDataAsync();
        var appointment = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow.AddDays(-1));
        appointment.Complete();
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();

        var plan = TreatmentPlan.Create(patient.Id, dentist.Id, appointment.Id, service.Id, service.Price, 1);
        plan.SetStatus(TreatmentPlanStatus.InProgress);
        _db.TreatmentPlans.Add(plan);
        await _db.SaveChangesAsync();

        var result = await _handler.GetDueAsync();

        result.Should().ContainSingle(x => x.OriginalAppointmentId == appointment.Id && x.PatientId == patient.Id);
    }

    /// <summary>Buổi gốc đã được check-in tái khám rồi (buổi con chưa kết thúc) phải bị ẩn khỏi danh sách
    /// chờ tái khám, tránh hiện trùng khi bệnh nhân đã có mặt.</summary>
    [Test]
    public async Task GetDueAsync_AlreadyCheckedInFollowUp_IsHiddenFromDueList()
    {
        var (patient, dentist, service) = await SeedBaseDataAsync();
        var appointment = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow.AddDays(-1));
        appointment.Complete();
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();
        var plan = TreatmentPlan.Create(patient.Id, dentist.Id, appointment.Id, service.Id, service.Price, 1);
        plan.SetStatus(TreatmentPlanStatus.InProgress);
        _db.TreatmentPlans.Add(plan);
        await _db.SaveChangesAsync();
        await _handler.CheckInAsync(appointment.Id);

        var result = await _handler.GetDueAsync();

        result.Should().NotContain(x => x.OriginalAppointmentId == appointment.Id);
    }

    /// <summary>Check-in tái khám cho buổi hẹn gốc không tồn tại phải báo lỗi.</summary>
    [Test]
    public async Task CheckInAsync_OriginalAppointmentNotFound_ThrowsNotFoundException()
    {
        Func<Task> act = () => _handler.CheckInAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>Chuỗi điều trị không còn liệu trình đang thực hiện thì không cho check-in tái khám.</summary>
    [Test]
    public async Task CheckInAsync_NoActiveTreatmentPlanInChain_ThrowsValidationException()
    {
        var (patient, dentist, _) = await SeedBaseDataAsync();
        var appointment = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow.AddDays(-1));
        appointment.Complete();
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();

        Func<Task> act = () => _handler.CheckInAsync(appointment.Id);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Buổi gốc đã có buổi tái khám check-in rồi (chưa hủy) thì không cho check-in lần 2.</summary>
    [Test]
    public async Task CheckInAsync_AlreadyCheckedIn_ThrowsConflictException()
    {
        var (patient, dentist, service) = await SeedBaseDataAsync();
        var appointment = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow.AddDays(-1));
        appointment.Complete();
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();

        var plan = TreatmentPlan.Create(patient.Id, dentist.Id, appointment.Id, service.Id, service.Price, 1);
        plan.SetStatus(TreatmentPlanStatus.InProgress);
        _db.TreatmentPlans.Add(plan);
        await _db.SaveChangesAsync();
        await _handler.CheckInAsync(appointment.Id);

        Func<Task> act = () => _handler.CheckInAsync(appointment.Id);

        await act.Should().ThrowAsync<ConflictException>();
    }

    /// <summary>Check-in tái khám hợp lệ phải tạo buổi hẹn mới ở trạng thái CheckedIn, gắn về buổi gốc.</summary>
    [Test]
    public async Task CheckInAsync_ValidRequest_CreatesCheckedInFollowUpAppointment()
    {
        var (patient, dentist, service) = await SeedBaseDataAsync();
        var appointment = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow.AddDays(-1));
        appointment.Complete();
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();

        var plan = TreatmentPlan.Create(patient.Id, dentist.Id, appointment.Id, service.Id, service.Price, 1);
        plan.SetStatus(TreatmentPlanStatus.InProgress);
        _db.TreatmentPlans.Add(plan);
        await _db.SaveChangesAsync();

        var newAppointmentId = await _handler.CheckInAsync(appointment.Id);

        var followUp = await _db.Appointments.SingleAsync(a => a.Id == newAppointmentId);
        followUp.Status.Should().Be(AppointmentStatus.CheckedIn);
        followUp.FollowUpFromAppointmentId.Should().Be(appointment.Id);
    }
}
