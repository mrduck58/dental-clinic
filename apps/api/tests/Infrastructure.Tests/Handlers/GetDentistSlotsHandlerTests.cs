using DentalClinic.API.Application.UseCases.Appointments;
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

        var result = (await _handler.HandleAsync(date)).ToList();

        result.Should().ContainSingle();
        var slots = result[0].Slots.ToDictionary(s => s.Range);

        slots["08:00 - 08:30"].IsBooked.Should().BeTrue();
        slots["08:30 - 09:00"].IsBooked.Should().BeTrue();
        slots["09:00 - 09:30"].IsBooked.Should().BeTrue();
        slots["09:30 - 10:00"].IsBooked.Should().BeFalse();

        slots.Values.Should().OnlyContain(s => s.Period == WorkShifts.PeriodMorning);
    }
}
