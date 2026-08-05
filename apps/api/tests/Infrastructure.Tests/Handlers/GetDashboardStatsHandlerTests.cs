using DentalClinic.API.Application.UseCases.Dashboard;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Handlers;

[TestFixture]
public class GetDashboardStatsHandlerTests
{
    private static readonly TimeZoneInfo VietnamTz = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

    private AppDbContext _db = null!;
    private GetDashboardStatsHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _handler = new GetDashboardStatsHandler(_db);
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    private async Task<(Patient patient, DentistProfile dentist)> SeedBasicDataAsync(
        string dentistName = "BS. Nguyễn Văn A", string specialization = "Nha khoa tổng quát")
    {
        var patientUser = User.Create("p1", $"p1-{Guid.NewGuid()}@test.com", "hash", UserRole.Patient, fullName: "Trần Thị B");
        var dentistUser = User.Create("d1", $"d1-{Guid.NewGuid()}@test.com", "hash", UserRole.Dentist, fullName: dentistName);
        _db.Users.AddRange(patientUser, dentistUser);

        var employee = Employee.Create(dentistUser.Id, $"DT-{Guid.NewGuid():N}");
        employee.User = dentistUser;
        var dentist = DentistProfile.Create(employee.Id, specialization, "N/A", 5);
        dentist.Employee = employee;
        var patient = Patient.Create(patientUser.Id, new DateOnly(1990, 1, 1), "Nữ");
        _db.Employees.Add(employee);
        _db.DentistProfiles.Add(dentist);
        _db.Patients.Add(patient);

        await _db.SaveChangesAsync();
        return (patient, dentist);
    }

    private static void SetCreatedAt<T>(T entity, DateTimeOffset value) =>
        typeof(T).GetProperty("CreatedAt")!.SetValue(entity, value);

    private static DateTimeOffset ToVn(DateOnly date) => new(date.Year, date.Month, date.Day, 0, 0, 0, VietnamTz.BaseUtcOffset);

    private static DateOnly TodayVn() => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTz));

    private static (DateOnly Start, DateOnly End) ThisWeek()
    {
        var today = TodayVn();
        var dow = (int)today.DayOfWeek;
        var daysFromMonday = dow == 0 ? 6 : dow - 1;
        var start = today.AddDays(-daysFromMonday);
        return (start, start.AddDays(7));
    }

    [Test]
    public async Task Handle_InvalidRange_ThrowsValidationException()
    {
        Func<Task> act = async () => await _handler.Handle(new GetDashboardStatsQuery("decade"), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Bệnh nhân được tạo trong tuần hiện tại phải được tính vào NewPatientsCount.</summary>
    [Test]
    public async Task Handle_PatientCreatedThisWeek_CountsAsNewPatient()
    {
        var user = User.CreateEmployee($"pnew-{Guid.NewGuid()}@test.com", UserRole.Patient, fullName: "Bệnh nhân mới");
        _db.Users.Add(user);
        var patient = Patient.Create(user.Id, new DateOnly(1995, 5, 5), "Nam");
        SetCreatedAt(patient, DateTimeOffset.UtcNow);
        _db.Patients.Add(patient);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetDashboardStatsQuery("week"), CancellationToken.None);

        result.NewPatientsCount.Should().Be(1);
    }

    /// <summary>Bệnh nhân được tạo cách đây hơn 1 năm không được tính vào kỳ hiện tại của bất kỳ range nào.</summary>
    [Test]
    public async Task Handle_PatientCreatedLongAgo_NotCountedInCurrentPeriod()
    {
        var user = User.CreateEmployee($"pold-{Guid.NewGuid()}@test.com", UserRole.Patient, fullName: "Bệnh nhân cũ");
        _db.Users.Add(user);
        var patient = Patient.Create(user.Id, new DateOnly(1980, 1, 1), "Nam");
        SetCreatedAt(patient, DateTimeOffset.UtcNow.AddDays(-400));
        _db.Patients.Add(patient);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetDashboardStatsQuery("year"), CancellationToken.None);

        result.NewPatientsCount.Should().Be(0);
    }

    /// <summary>Lịch hẹn đã hủy không được tính vào AppointmentsCount.</summary>
    [Test]
    public async Task Handle_CancelledAppointment_ExcludedFromCount()
    {
        var (patient, dentist) = await SeedBasicDataAsync();
        var cancelled = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        cancelled.Cancel();
        var confirmed = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        confirmed.Confirm();
        _db.Appointments.AddRange(cancelled, confirmed);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetDashboardStatsQuery("week"), CancellationToken.None);

        result.AppointmentsCount.Should().Be(1);
    }

    /// <summary>Doanh thu chỉ tính hóa đơn đã thanh toán (Paid), bỏ qua hóa đơn Unpaid.</summary>
    [Test]
    public async Task Handle_OnlyPaidInvoices_CountedInRevenue()
    {
        var (patient, dentist) = await SeedBasicDataAsync();
        var appt1 = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        var appt2 = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        _db.Appointments.AddRange(appt1, appt2);
        await _db.SaveChangesAsync();

        var paidInvoice = Invoice.Issue(appt1.Id, "INV001", [("Khám tổng quát", 1, 500_000m, null, 500_000m)], 0, PaymentMethod.Cash);
        paidInvoice.MarkAsPaid(PaymentMethod.Cash);
        typeof(Invoice).GetProperty("PaymentDate")!.SetValue(paidInvoice, DateTimeOffset.UtcNow);

        var unpaidInvoice = Invoice.Issue(appt2.Id, "INV002", [("Khám tổng quát", 1, 300_000m, null, 300_000m)], 0, PaymentMethod.Cash);

        _db.Invoices.AddRange(paidInvoice, unpaidInvoice);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetDashboardStatsQuery("week"), CancellationToken.None);

        result.RevenueAmount.Should().Be(500_000m);
    }

    /// <summary>Kỳ trước = 0, kỳ hiện tại > 0 → tăng trưởng 100%.</summary>
    [Test]
    public async Task Handle_PreviousPeriodZero_TrendIsHundredPercent()
    {
        var user = User.CreateEmployee($"pnew2-{Guid.NewGuid()}@test.com", UserRole.Patient, fullName: "Bệnh nhân mới");
        _db.Users.Add(user);
        var patient = Patient.Create(user.Id, new DateOnly(1995, 5, 5), "Nam");
        SetCreatedAt(patient, DateTimeOffset.UtcNow);
        _db.Patients.Add(patient);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetDashboardStatsQuery("week"), CancellationToken.None);

        result.NewPatientsTrendPercent.Should().Be(100);
    }

    /// <summary>Không có dữ liệu ở cả 2 kỳ → tăng trưởng 0%, không chia cho 0.</summary>
    [Test]
    public async Task Handle_NoDataEitherPeriod_TrendIsZero()
    {
        var result = await _handler.Handle(new GetDashboardStatsQuery("week"), CancellationToken.None);

        result.NewPatientsTrendPercent.Should().Be(0);
    }

    /// <summary>Không truyền range (null) phải mặc định về "week" thay vì ném lỗi.</summary>
    [Test]
    public async Task Handle_NullRange_DefaultsToWeek()
    {
        var result = await _handler.Handle(new GetDashboardStatsQuery(null), CancellationToken.None);

        result.Range.Should().Be("week");
    }

    /// <summary>Kỳ hiện tại giảm so với kỳ trước phải cho ra % tăng trưởng âm (không phải trị tuyệt đối).</summary>
    [Test]
    public async Task Handle_CurrentPeriodLowerThanPrevious_TrendIsNegative()
    {
        var (patient, dentist) = await SeedBasicDataAsync();
        var (weekStart, _) = ThisWeek();
        // Kỳ trước (tuần trước): 2 lịch hẹn hợp lệ.
        _db.Appointments.AddRange(
            Appointment.Create(patient.Id, dentist.Id, ToVn(weekStart.AddDays(-7)).AddHours(9)),
            Appointment.Create(patient.Id, dentist.Id, ToVn(weekStart.AddDays(-6)).AddHours(9)),
            // Kỳ hiện tại (tuần này): 1 lịch hẹn.
            Appointment.Create(patient.Id, dentist.Id, ToVn(weekStart).AddHours(9)));
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetDashboardStatsQuery("week"), CancellationToken.None);

        result.AppointmentsCount.Should().Be(1);
        result.AppointmentsTrendPercent.Should().BeLessThan(0);
    }
}
