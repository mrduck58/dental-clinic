using DentalClinic.API.Application.UseCases.Appointments;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Handlers;

[TestFixture]
public class GetWaitingQueueHandlerTests
{
    private AppDbContext _db = null!;
    private GetWaitingQueueHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _handler = new GetWaitingQueueHandler(_db);
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    private static readonly TimeZoneInfo VietnamTz = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
    private static DateTimeOffset NowVietnam() => TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, VietnamTz);
    private static string CurrentShiftCode() => NowVietnam().Hour < 12 ? "morning" : "afternoon";

    /// <summary>Ngày không có lịch làm việc và không có lịch hẹn nào phải trả về danh sách phòng rỗng.</summary>
    [Test]
    public async Task HandleAsync_NoScheduleOrAppointments_ReturnsNoRooms()
    {
        var result = await _handler.HandleAsync(DateOnly.FromDateTime(DateTime.Today));

        result.Rooms.Should().BeEmpty();
        result.TotalWaiting.Should().Be(0);
    }

    /// <summary>Bệnh nhân đã check-in phải được xếp vào đúng phòng của bác sĩ phụ trách theo ca làm việc.</summary>
    [Test]
    public async Task HandleAsync_CheckedInPatient_IsGroupedUnderDentistRoom()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var dentistUser = User.Create("wq1", $"wq1-{Guid.NewGuid()}@test.com", "hash", "Dentist");
        _db.Users.Add(dentistUser);
        var dentist = Dentist.Create(dentistUser.Id, "Nha khoa tổng quát", 5);
        var patient = Patient.Create(Guid.Empty, new DateOnly(1990, 1, 1), "Nam");
        _db.Dentists.Add(dentist);
        _db.Patients.Add(patient);
        _db.WorkSchedules.Add(WorkSchedule.Create(
            today, "morning", "dentist", "dentist", dentist.FullName, "Phòng 101", "border-primary", false));
        var appointment = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        appointment.CheckIn();
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();

        var result = await _handler.HandleAsync(today);

        result.Rooms.Should().ContainSingle(r => r.RoomName == "Phòng 101");
        var room = result.Rooms.Single(r => r.RoomName == "Phòng 101");
        room.Patients.Should().ContainSingle(p => p.PatientName == "Bệnh nhân Chờ");
        room.Dentists.Should().ContainSingle(d => d.DentistName == "BS Hàng Đợi");
    }

    /// <summary>Bệnh nhân đang khám (InProgress) phải được xếp đứng đầu hàng đợi, trước cả những
    /// người đã check-in sớm hơn.</summary>
    [Test]
    public async Task HandleAsync_InProgressPatient_IsListedBeforeCheckedInPatients()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var dentistUser = User.Create("wq2", $"wq2-{Guid.NewGuid()}@test.com", "hash", "Dentist");
        _db.Users.Add(dentistUser);
        var dentist = Dentist.Create(dentistUser.Id, "Nha khoa tổng quát", 5);
        var patientA = Patient.Create(Guid.Empty, new DateOnly(1990, 1, 1), "Nam");
        var patientB = Patient.Create(Guid.Empty, new DateOnly(1991, 1, 1), "Nữ");
        _db.Dentists.Add(dentist);
        _db.Patients.AddRange(patientA, patientB);
        _db.WorkSchedules.Add(WorkSchedule.Create(
            today, "morning", "dentist", "dentist", dentist.FullName, "Phòng 202", "border-primary", false));
        var waiting = Appointment.Create(patientA.Id, dentist.Id, DateTimeOffset.UtcNow.AddMinutes(-30));
        waiting.CheckIn();
        var inProgress = Appointment.Create(patientB.Id, dentist.Id, DateTimeOffset.UtcNow.AddMinutes(-5));
        inProgress.CheckIn();
        inProgress.StartTreatment();
        _db.Appointments.AddRange(waiting, inProgress);
        await _db.SaveChangesAsync();

        var result = await _handler.HandleAsync(today);

        var room = result.Rooms.Single(r => r.RoomName == "Phòng 202");
        room.Patients[0].PatientName.Should().Be("Bệnh nhân Đang Khám");
        room.Patients[1].PatientName.Should().Be("Bệnh nhân Chờ Trước");
    }

    /// <summary>Tổng số đếm ở cấp toàn hệ thống (chờ/đang khám/hoàn thành) phải khớp với trạng thái
    /// thực tế của các lịch hẹn trong ngày.</summary>
    [Test]
    public async Task HandleAsync_TotalCounts_MatchAppointmentStatuses()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var dentistUser = User.Create("wq3", $"wq3-{Guid.NewGuid()}@test.com", "hash", "Dentist");
        _db.Users.Add(dentistUser);
        var dentist = Dentist.Create(dentistUser.Id, "Nha khoa tổng quát", 5);
        var patient = Patient.Create(Guid.Empty, new DateOnly(1990, 1, 1), "Nam");
        _db.Dentists.Add(dentist);
        _db.Patients.Add(patient);
        var waiting = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        waiting.CheckIn();
        var completed = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow.AddHours(-2));
        completed.Complete();
        _db.Appointments.AddRange(waiting, completed);
        await _db.SaveChangesAsync();

        var result = await _handler.HandleAsync(today);

        result.TotalWaiting.Should().Be(1);
        result.TotalCompleted.Should().Be(1);
        result.TotalInProgress.Should().Be(0);
    }

    /// <summary>Bác sĩ có ca làm việc bao trùm thời điểm hiện tại phải được đánh dấu IsOnShiftNow = true.</summary>
    [Test]
    public async Task HandleAsync_DentistOnShiftNow_IsOnShiftNowIsTrue()
    {
        var today = DateOnly.FromDateTime(NowVietnam().DateTime);
        var currentShift = CurrentShiftCode();
        var dentistUser = User.Create("wq4", $"wq4-{Guid.NewGuid()}@test.com", "hash", "Dentist");
        _db.Users.Add(dentistUser);
        var dentist = Dentist.Create(dentistUser.Id, "Nha khoa tổng quát", 5);
        _db.Dentists.Add(dentist);
        _db.WorkSchedules.Add(WorkSchedule.Create(
            today, currentShift, "dentist", "dentist", dentist.FullName, "Phòng Trực", "border-primary", false));
        await _db.SaveChangesAsync();

        var result = await _handler.HandleAsync(today);

        var room = result.Rooms.Single(r => r.RoomName == "Phòng Trực");
        room.Dentists.Should().ContainSingle(d => d.IsOnShiftNow);
    }

    /// <summary>Bác sĩ có bệnh nhân đã check-in nhưng KHÔNG có ca trong bảng lịch làm việc (lịch bị xóa/thiếu)
    /// vẫn phải được hiện trong hàng đợi — dự phòng để hàng đợi không biến mất.</summary>
    [Test]
    public async Task HandleAsync_DentistWithCheckedInPatientButNoSchedule_StillShownInQueue()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var dentistUser = User.Create("wq5", $"wq5-{Guid.NewGuid()}@test.com", "hash", "Dentist");
        _db.Users.Add(dentistUser);
        var dentist = Dentist.Create(dentistUser.Id, "Nha khoa tổng quát", 5);
        var patient = Patient.Create(Guid.Empty, new DateOnly(1990, 1, 1), "Nam");
        _db.Dentists.Add(dentist);
        _db.Patients.Add(patient);
        var appointment = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        appointment.CheckIn();
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();

        var result = await _handler.HandleAsync(today);

        result.Rooms.Should().ContainSingle(r => r.RoomName == null);
        result.Rooms[0].Dentists.Should().ContainSingle(d => d.DentistName == "BS Không Ca");
        result.Rooms[0].Patients.Should().ContainSingle(p => p.PatientName == "Bệnh nhân Dự Phòng");
    }
}
