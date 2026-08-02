using DentalClinic.API.Application.UseCases.Dentists;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Schedules;
using DentalClinic.API.Infrastructure.Persistence;
using DentalClinic.API.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Handlers;

[TestFixture]
public class GetDentistSlotsHandlerTests
{
    private AppDbContext _db = null!;
    private GetDentistSlotsHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _handler = new GetDentistSlotsHandler(_db, new AppointmentRepository(_db));
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    /// <summary>
    /// Lịch hẹn 90 phút bắt đầu 08:00 phải chiếm cả 08:00/08:30/09:00 (bước 30 phút) —
    /// chỉ chặn đúng khung 08:00 sẽ để lọt việc đặt trùng vào các khung sau đó.
    /// </summary>
    [Test]
    public async Task HandleAsync_LongServiceBooking_BlocksAllOverlappingSlots()
    {
        var date = DateOnly.FromDateTime(DateTime.Today.AddDays(1));

        var dentistUser = User.Create("d1", "d1@test.com", "hash", "Dentist", fullName: "BS. Test");
        var patientUser = User.Create("p1", "p1@test.com", "hash", "Patient", fullName: "Bệnh nhân Test");
        _db.Users.AddRange(dentistUser, patientUser);

        var dentist = Dentist.Create(dentistUser.Id, "Nha khoa tổng quát", 5);
        var patient = Patient.Create(patientUser.Id, new DateOnly(1990, 1, 1), "Nam");
        _db.Dentists.Add(dentist);
        _db.Patients.Add(patient);

        var service = Service.Create("Điều trị tủy", 500_000, 90, "Dịch vụ dài");
        _db.Services.Add(service);

        var workSchedule = WorkSchedule.Create(date, "08:00-10:00", "dentist", "Dentist", dentist.FullName, "P1", "#fff", false);
        _db.WorkSchedules.Add(workSchedule);

        // 08:00 giờ VN (UTC+7) — khớp cách handler quy đổi AppointmentDate.UtcDateTime.AddHours(7)
        var appointmentDate = new DateTimeOffset(date.Year, date.Month, date.Day, 8, 0, 0, TimeSpan.FromHours(7));
        var appointment = Appointment.Create(patient.Id, dentist.Id, appointmentDate, serviceId: service.Id);
        _db.Appointments.Add(appointment);

        await _db.SaveChangesAsync();

        var result = (await _handler.Handle(new GetDentistSlotsQuery(date), CancellationToken.None)).ToList();

        result.Should().ContainSingle();
        var slots = result[0].Slots.ToDictionary(s => s.Range);

        slots["08:00 - 08:30"].IsBooked.Should().BeTrue();
        slots["08:30 - 09:00"].IsBooked.Should().BeTrue();
        slots["09:00 - 09:30"].IsBooked.Should().BeTrue();
        slots["09:30 - 10:00"].IsBooked.Should().BeFalse();

        slots.Values.Should().OnlyContain(s => s.Period == WorkShifts.PeriodMorning);
    }

    /// <summary>Ngày được đánh dấu nghỉ lễ (IsHoliday) phải trả về danh sách rỗng, không hiển thị
    /// bất kỳ bác sĩ hay khung giờ nào để đặt lịch.</summary>
    [Test]
    public async Task HandleAsync_HolidaySchedule_ReturnsEmptyList()
    {
        var date = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        _db.WorkSchedules.Add(WorkSchedule.Create(date, "", "holiday", "holiday", "", "", "", true));
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetDentistSlotsQuery(date), CancellationToken.None);

        result.Should().BeEmpty();
    }

    /// <summary>Chủ Nhật không có WorkSchedule nào phải trả về rỗng — mặc định không làm việc Chủ Nhật.</summary>
    [Test]
    public async Task HandleAsync_SundayWithNoSchedule_ReturnsEmptyList()
    {
        var sunday = Enumerable.Range(1, 14)
            .Select(i => DateOnly.FromDateTime(DateTime.Today.AddDays(i)))
            .First(d => d.DayOfWeek == DayOfWeek.Sunday);

        var result = await _handler.Handle(new GetDentistSlotsQuery(sunday), CancellationToken.None);

        result.Should().BeEmpty();
    }

    /// <summary>Ngày thường (không phải Chủ Nhật) không có WorkSchedule nào cũng phải trả về rỗng —
    /// không cho đặt lịch khi chưa có phân ca.</summary>
    [Test]
    public async Task HandleAsync_WeekdayWithNoSchedule_ReturnsEmptyList()
    {
        var weekday = Enumerable.Range(1, 14)
            .Select(i => DateOnly.FromDateTime(DateTime.Today.AddDays(i)))
            .First(d => d.DayOfWeek != DayOfWeek.Sunday);

        var result = await _handler.Handle(new GetDentistSlotsQuery(weekday), CancellationToken.None);

        result.Should().BeEmpty();
    }

    /// <summary>
    /// Bác sĩ được phân ca và chưa có lịch hẹn nào phải xuất hiện với đầy đủ thông tin DTO
    /// (tên, chuyên khoa, số năm kinh nghiệm) và toàn bộ slot đều chưa được đặt.
    /// </summary>
    [Test]
    public async Task HandleAsync_DentistScheduledWithNoBookings_ReturnsDentistWithAllSlotsFree()
    {
        var date = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        var dentistUser = User.Create("d2", "d2@test.com", "hash", "Dentist");
        _db.Users.Add(dentistUser);
        var dentist = Dentist.Create(dentistUser.Id, "Implant", 7);
        _db.Dentists.Add(dentist);
        _db.WorkSchedules.Add(WorkSchedule.Create(date, "08:00-10:00", "dentist", "Dentist", dentist.FullName, "P2", "#fff", false));
        await _db.SaveChangesAsync();

        var result = (await _handler.Handle(new GetDentistSlotsQuery(date), CancellationToken.None)).ToList();

        var dto = result.Should().ContainSingle().Subject;
        dto.FullName.Should().Be("BS. Free");
        dto.Specialization.Should().Be("Implant");
        dto.ExperienceYears.Should().Be(7);
        dto.Slots.Should().OnlyContain(s => !s.IsBooked);
    }

    /// <summary>
    /// Chỉ những bác sĩ có tên khớp với WorkSchedule của ngày đó mới được trả về — bác sĩ tồn tại
    /// trong hệ thống nhưng không được phân ca hôm đó không được xuất hiện trong danh sách đặt lịch.
    /// </summary>
    [Test]
    public async Task HandleAsync_DentistWithoutScheduleOnThatDay_IsExcludedFromResult()
    {
        var date = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        var scheduledUser = User.Create("d3", "d3@test.com", "hash", "Dentist");
        var unscheduledUser = User.Create("d4", "d4@test.com", "hash", "Dentist");
        _db.Users.AddRange(scheduledUser, unscheduledUser);
        var scheduledDentist = Dentist.Create(scheduledUser.Id, "Nha khoa tổng quát", 5);
        var unscheduledDentist = Dentist.Create(unscheduledUser.Id, "Nha khoa tổng quát", 5);
        _db.Dentists.AddRange(scheduledDentist, unscheduledDentist);
        _db.WorkSchedules.Add(WorkSchedule.Create(date, "08:00-10:00", "dentist", "Dentist", scheduledDentist.FullName, "P1", "#fff", false));
        await _db.SaveChangesAsync();

        var result = (await _handler.Handle(new GetDentistSlotsQuery(date), CancellationToken.None)).ToList();

        result.Should().ContainSingle(d => d.FullName == "BS. Có ca");
    }
}
