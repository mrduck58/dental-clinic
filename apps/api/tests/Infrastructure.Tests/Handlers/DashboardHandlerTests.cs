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
public class DashboardHandlerTests
{
    private static readonly TimeZoneInfo VietnamTz = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

    private AppDbContext _db = null!;
    private DashboardHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _handler = new DashboardHandler(_db);
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    // ── Seed helpers ──────────────────────────────────────────────────────────

    private async Task<(Patient patient, Dentist dentist)> SeedBasicDataAsync(
        string dentistName = "BS. Nguyễn Văn A", string specialization = "Nha khoa tổng quát")
    {
        var patientUser = User.Create("p1", $"p1-{Guid.NewGuid()}@test.com", "hash", "Patient");
        var dentistUser = User.Create("d1", $"d1-{Guid.NewGuid()}@test.com", "hash", "Dentist");
        _db.Users.AddRange(patientUser, dentistUser);

        var dentist = Dentist.Create(dentistUser.Id, dentistName, specialization, 5);
        var patient = Patient.Create("Trần Thị B", new DateOnly(1990, 1, 1), "Nữ", patientUser.Id);
        _db.Dentists.Add(dentist);
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

    // ── GetStatsAsync ─────────────────────────────────────────────────────────

    [Test]
    public async Task GetStatsAsync_InvalidRange_ThrowsValidationException()
    {
        Func<Task> act = async () => await _handler.GetStatsAsync("decade");

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Bệnh nhân được tạo trong tuần hiện tại phải được tính vào NewPatientsCount.</summary>
    [Test]
    public async Task GetStatsAsync_PatientCreatedThisWeek_CountsAsNewPatient()
    {
        var patient = Patient.Create("Bệnh nhân mới", new DateOnly(1995, 5, 5), "Nam");
        SetCreatedAt(patient, DateTimeOffset.UtcNow);
        _db.Patients.Add(patient);
        await _db.SaveChangesAsync();

        var result = await _handler.GetStatsAsync("week");

        result.NewPatientsCount.Should().Be(1);
    }

    /// <summary>Bệnh nhân được tạo cách đây hơn 1 năm không được tính vào kỳ hiện tại của bất kỳ range nào.</summary>
    [Test]
    public async Task GetStatsAsync_PatientCreatedLongAgo_NotCountedInCurrentPeriod()
    {
        var patient = Patient.Create("Bệnh nhân cũ", new DateOnly(1980, 1, 1), "Nam");
        SetCreatedAt(patient, DateTimeOffset.UtcNow.AddDays(-400));
        _db.Patients.Add(patient);
        await _db.SaveChangesAsync();

        var result = await _handler.GetStatsAsync("year");

        result.NewPatientsCount.Should().Be(0);
    }

    /// <summary>Lịch hẹn đã hủy không được tính vào AppointmentsCount.</summary>
    [Test]
    public async Task GetStatsAsync_CancelledAppointment_ExcludedFromCount()
    {
        var (patient, dentist) = await SeedBasicDataAsync();
        var cancelled = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        cancelled.Cancel();
        var confirmed = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        confirmed.Confirm();
        _db.Appointments.AddRange(cancelled, confirmed);
        await _db.SaveChangesAsync();

        var result = await _handler.GetStatsAsync("week");

        result.AppointmentsCount.Should().Be(1);
    }

    /// <summary>Doanh thu chỉ tính hóa đơn đã thanh toán (Paid), bỏ qua hóa đơn Unpaid.</summary>
    [Test]
    public async Task GetStatsAsync_OnlyPaidInvoices_CountedInRevenue()
    {
        var (patient, dentist) = await SeedBasicDataAsync();
        var appt1 = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        var appt2 = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        _db.Appointments.AddRange(appt1, appt2);
        await _db.SaveChangesAsync();

        var paidInvoice = Invoice.Issue(appt1.Id, "INV001", [("Khám tổng quát", 1, 500_000m)], 0, PaymentMethod.Cash, PaymentType.Full, 500_000m);
        paidInvoice.MarkAsPaid(PaymentMethod.Cash);
        typeof(Invoice).GetProperty("PaymentDate")!.SetValue(paidInvoice, DateTimeOffset.UtcNow);

        var unpaidInvoice = Invoice.Issue(appt2.Id, "INV002", [("Khám tổng quát", 1, 300_000m)], 0, PaymentMethod.Cash, PaymentType.Full, 300_000m);

        _db.Invoices.AddRange(paidInvoice, unpaidInvoice);
        await _db.SaveChangesAsync();

        var result = await _handler.GetStatsAsync("week");

        result.RevenueAmount.Should().Be(500_000m);
    }

    /// <summary>Kỳ trước = 0, kỳ hiện tại > 0 → tăng trưởng 100%.</summary>
    [Test]
    public async Task GetStatsAsync_PreviousPeriodZero_TrendIsHundredPercent()
    {
        var patient = Patient.Create("Bệnh nhân mới", new DateOnly(1995, 5, 5), "Nam");
        SetCreatedAt(patient, DateTimeOffset.UtcNow);
        _db.Patients.Add(patient);
        await _db.SaveChangesAsync();

        var result = await _handler.GetStatsAsync("week");

        result.NewPatientsTrendPercent.Should().Be(100);
    }

    /// <summary>Không có dữ liệu ở cả 2 kỳ → tăng trưởng 0%, không chia cho 0.</summary>
    [Test]
    public async Task GetStatsAsync_NoDataEitherPeriod_TrendIsZero()
    {
        var result = await _handler.GetStatsAsync("week");

        result.NewPatientsTrendPercent.Should().Be(0);
    }

    // ── GetAppointmentTrendAsync ─────────────────────────────────────────────

    [Test]
    public async Task GetAppointmentTrendAsync_InvalidRange_ThrowsValidationException()
    {
        Func<Task> act = async () => await _handler.GetAppointmentTrendAsync("century");

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task GetAppointmentTrendAsync_WeekRange_ReturnsSevenBuckets()
    {
        var result = await _handler.GetAppointmentTrendAsync("week");

        result.Points.Should().HaveCount(7);
    }

    /// <summary>Tổng số lịch hẹn trong các bucket phải bằng tổng lịch hẹn hợp lệ (không hủy) trong tuần.</summary>
    [Test]
    public async Task GetAppointmentTrendAsync_WeekRange_BucketsSumMatchesTotalAppointments()
    {
        var (patient, dentist) = await SeedBasicDataAsync();
        var (weekStart, _) = ThisWeek();
        _db.Appointments.AddRange(
            Appointment.Create(patient.Id, dentist.Id, ToVn(weekStart).AddHours(9)),
            Appointment.Create(patient.Id, dentist.Id, ToVn(weekStart).AddHours(10)),
            Appointment.Create(patient.Id, dentist.Id, ToVn(weekStart.AddDays(2)).AddHours(9)));
        await _db.SaveChangesAsync();

        var result = await _handler.GetAppointmentTrendAsync("week");

        result.Points.Sum(p => p.Count).Should().Be(3);
    }

    [Test]
    public async Task GetAppointmentTrendAsync_YearRange_ReturnsTwelveBuckets()
    {
        var result = await _handler.GetAppointmentTrendAsync("year");

        result.Points.Should().HaveCount(12);
    }

    // ── GetServiceDistributionAsync ──────────────────────────────────────────

    /// <summary>Phân bổ dịch vụ phải nhóm đúng theo Service và tính % chính xác trên tổng.</summary>
    [Test]
    public async Task GetServiceDistributionAsync_GroupsByServiceAndComputesPercentage()
    {
        var (patient, dentist) = await SeedBasicDataAsync();
        var service = Service.Create("Cấy ghép Implant", 10_000_000m, 60, "Mô tả");
        _db.Services.Add(service);
        _db.Appointments.AddRange(
            Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow, serviceId: service.Id),
            Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow, serviceId: service.Id),
            Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow)); // không có dịch vụ
        await _db.SaveChangesAsync();

        var result = await _handler.GetServiceDistributionAsync("week", topN: 5);

        result.TotalAppointments.Should().Be(3);
        var serviceItem = result.Items.Single(i => i.ServiceId == service.Id);
        serviceItem.ServiceName.Should().Be("Cấy ghép Implant");
        serviceItem.Count.Should().Be(2);
        serviceItem.Percentage.Should().BeApproximately(66.7, 0.1);
    }

    /// <summary>Khi số dịch vụ vượt topN, phần còn lại phải gộp vào 1 mục "khác" (ServiceId null).</summary>
    [Test]
    public async Task GetServiceDistributionAsync_ExceedsTopN_AggregatesRestIntoOtherBucket()
    {
        var (patient, dentist) = await SeedBasicDataAsync();
        var services = Enumerable.Range(1, 3)
            .Select(i => Service.Create($"Dịch vụ {i}", 100_000m, 30, "Mô tả"))
            .ToList();
        _db.Services.AddRange(services);
        foreach (var service in services)
            _db.Appointments.Add(Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow, serviceId: service.Id));
        await _db.SaveChangesAsync();

        var result = await _handler.GetServiceDistributionAsync("week", topN: 1);

        result.Items.Should().HaveCount(2);
        result.Items.Should().ContainSingle(i => i.ServiceId == null && i.Count == 2);
    }

    [Test]
    public async Task GetServiceDistributionAsync_NoAppointments_ReturnsEmptyItems()
    {
        var result = await _handler.GetServiceDistributionAsync("week", topN: 5);

        result.TotalAppointments.Should().Be(0);
        result.Items.Should().BeEmpty();
    }

    // ── GetTodayAppointmentsAsync ─────────────────────────────────────────────

    [Test]
    public async Task GetTodayAppointmentsAsync_OnlyReturnsAppointmentsScheduledToday()
    {
        var (patient, dentist) = await SeedBasicDataAsync();
        _db.Appointments.AddRange(
            Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow),
            Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow.AddDays(3)));
        await _db.SaveChangesAsync();

        var result = await _handler.GetTodayAppointmentsAsync(1, 10);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle();
    }

    [Test]
    public async Task GetTodayAppointmentsAsync_RespectsPagination()
    {
        var (patient, dentist) = await SeedBasicDataAsync();
        for (var i = 0; i < 5; i++)
            _db.Appointments.Add(Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow.AddMinutes(i)));
        await _db.SaveChangesAsync();

        var result = await _handler.GetTodayAppointmentsAsync(2, 2);

        result.TotalCount.Should().Be(5);
        result.Items.Should().HaveCount(2);
        result.TotalPages.Should().Be(3);
    }

    // ── GetWeeklyScheduleAsync ────────────────────────────────────────────────

    [Test]
    public async Task GetWeeklyScheduleAsync_ReturnsSevenDaysWithCorrectTodayFlag()
    {
        var result = await _handler.GetWeeklyScheduleAsync(null);

        result.Week.Should().HaveCount(7);
        result.Week.Should().ContainSingle(d => d.IsToday);
    }

    /// <summary>Bác sĩ có lịch hẹn đang InProgress trong ngày phải được đánh dấu IsBusy = true.</summary>
    [Test]
    public async Task GetWeeklyScheduleAsync_DentistWithInProgressAppointment_MarkedAsBusy()
    {
        var (patient, dentist) = await SeedBasicDataAsync(dentistName: "BS. Nguyễn Minh Đức");
        var today = TodayVn();

        _db.WorkSchedules.Add(WorkSchedule.Create(
            today, "morning", "dentist", "dentist", dentist.FullName, "P101", "border-primary", false));

        var appt = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        appt.Confirm();
        appt.CheckIn();
        appt.StartTreatment();
        _db.Appointments.Add(appt);
        await _db.SaveChangesAsync();

        var result = await _handler.GetWeeklyScheduleAsync(today);

        result.MorningShift.Should().ContainSingle();
        result.MorningShift.Single().StaffName.Should().Be(dentist.FullName);
        result.MorningShift.Single().IsBusy.Should().BeTrue();
    }

    /// <summary>Bác sĩ không có lịch hẹn đang khám phải được đánh dấu IsBusy = false.</summary>
    [Test]
    public async Task GetWeeklyScheduleAsync_DentistWithoutInProgressAppointment_NotBusy()
    {
        var (_, dentist) = await SeedBasicDataAsync(dentistName: "BS. Lê Thị Phương Thảo");
        var today = TodayVn();

        _db.WorkSchedules.Add(WorkSchedule.Create(
            today, "afternoon", "dentist", "dentist", dentist.FullName, "P102", "border-secondary", false));
        await _db.SaveChangesAsync();

        var result = await _handler.GetWeeklyScheduleAsync(today);

        result.AfternoonShift.Should().ContainSingle();
        result.AfternoonShift.Single().IsBusy.Should().BeFalse();
    }

    /// <summary>Ca trực đánh dấu ngày nghỉ (IsHoliday) không được xuất hiện trong danh sách ca trực.</summary>
    [Test]
    public async Task GetWeeklyScheduleAsync_HolidayShift_ExcludedFromResults()
    {
        var (_, dentist) = await SeedBasicDataAsync();
        var today = TodayVn();
        _db.WorkSchedules.Add(WorkSchedule.Create(
            today, "morning", "dentist", "dentist", dentist.FullName, "P101", "border-primary", true));
        await _db.SaveChangesAsync();

        var result = await _handler.GetWeeklyScheduleAsync(today);

        result.MorningShift.Should().BeEmpty();
    }

    // ── GetRecentFeedbackAsync ────────────────────────────────────────────────

    /// <summary>Chỉ đánh giá ở trạng thái Featured mới được trả về, bỏ qua Pending/Hidden.</summary>
    [Test]
    public async Task GetRecentFeedbackAsync_OnlyReturnsFeaturedFeedback()
    {
        var featured = Feedback.Create("Nguyễn Thị Thu Hà", 5, "Rất hài lòng");
        featured.Feature();
        var pending = Feedback.Create("Trần Hoàng Nam", 4, "Tạm ổn");
        var hidden = Feedback.Create("Phạm Minh Anh", 1, "Không hài lòng");
        hidden.Hide();
        _db.Feedbacks.AddRange(featured, pending, hidden);
        await _db.SaveChangesAsync();

        var result = await _handler.GetRecentFeedbackAsync(5);

        result.TotalFeaturedCount.Should().Be(1);
        result.Items.Should().ContainSingle(i => i.CustomerName == "Nguyễn Thị Thu Hà");
    }

    [Test]
    public async Task GetRecentFeedbackAsync_ComputesAverageRatingAcrossFeaturedOnly()
    {
        var f1 = Feedback.Create("KH 1", 5, "Tốt");
        f1.Feature();
        var f2 = Feedback.Create("KH 2", 3, "Bình thường");
        f2.Feature();
        _db.Feedbacks.AddRange(f1, f2);
        await _db.SaveChangesAsync();

        var result = await _handler.GetRecentFeedbackAsync(5);

        result.AverageRating.Should().Be(4);
    }

    [Test]
    public async Task GetRecentFeedbackAsync_RespectsLimit()
    {
        for (var i = 0; i < 5; i++)
        {
            var fb = Feedback.Create($"KH {i}", 5, "Tốt");
            fb.Feature();
            _db.Feedbacks.Add(fb);
        }
        await _db.SaveChangesAsync();

        var result = await _handler.GetRecentFeedbackAsync(2);

        result.Items.Should().HaveCount(2);
        result.TotalFeaturedCount.Should().Be(5);
    }

    [Test]
    public async Task GetRecentFeedbackAsync_NoFeaturedFeedback_ReturnsZeroAverage()
    {
        var result = await _handler.GetRecentFeedbackAsync(5);

        result.AverageRating.Should().Be(0);
        result.Items.Should().BeEmpty();
    }
}
