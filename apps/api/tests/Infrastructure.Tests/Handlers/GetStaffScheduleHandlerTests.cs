using DentalClinic.API.Application.UseCases.Appointments;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Handlers;

[TestFixture]
public class GetStaffScheduleHandlerTests
{
    private AppDbContext _db = null!;
    private GetStaffScheduleHandler _handler = null!;
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _handler = new GetStaffScheduleHandler(_db);
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    private async Task<User> SeedActiveDentistUserAsync(string fullName)
    {
        // User.Create mặc định EmploymentStatus = "Active".
        var user = User.Create($"u-{Guid.NewGuid()}", $"{Guid.NewGuid()}@test.com", "hash", "Dentist", fullName: fullName);
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    /// <summary>
    /// Bác sĩ chỉ có dòng WorkSchedule với Shift không hợp lệ (không phải morning/afternoon —
    /// dữ liệu rác/cũ) không được coi là đang làm việc hôm nay, nên không được xuất hiện trong
    /// danh sách đặt lịch tại quầy.
    /// </summary>
    [Test]
    public async Task HandleAsync_DentistWithOnlyInvalidShiftValue_ExcludedFromResult()
    {
        var user = await SeedActiveDentistUserAsync("Dentist Test");
        _db.WorkSchedules.Add(WorkSchedule.Create(
            Today, "08:00-10:00", "dentist", "dentist", user.FullName!, "Phòng 2", "border-primary", false));
        await _db.SaveChangesAsync();

        var result = await _handler.HandleAsync(Today);

        result.Dentists.Should().BeEmpty();
    }

    /// <summary>
    /// Bác sĩ có ca làm việc hợp lệ ("morning") phải xuất hiện trong danh sách,
    /// với morningSlots đầy đủ và afternoonSlots rỗng (không làm ca chiều).
    /// </summary>
    [Test]
    public async Task HandleAsync_DentistWithValidMorningShift_IncludedWithMorningSlotsOnly()
    {
        var user = await SeedActiveDentistUserAsync("BS. Nguyễn Văn Hùng");
        _db.WorkSchedules.Add(WorkSchedule.Create(
            Today, "morning", "dentist", "dentist", user.FullName!, "Phòng 1", "border-primary", false));
        await _db.SaveChangesAsync();

        var result = await _handler.HandleAsync(Today);

        var dentist = result.Dentists.Should().ContainSingle().Subject;
        dentist.Name.Should().Be("BS. Nguyễn Văn Hùng");
        dentist.MorningSlots.Should().NotBeEmpty();
        dentist.AfternoonSlots.Should().BeEmpty();
    }

    /// <summary>
    /// Khi một bác sĩ có cả dòng hợp lệ ("morning") lẫn dòng rác ("10:00-12:00") trong cùng ngày,
    /// dòng rác phải bị bỏ qua — bác sĩ vẫn chỉ hiện đúng ca morning.
    /// </summary>
    [Test]
    public async Task HandleAsync_DentistWithMixOfValidAndInvalidShift_IgnoresInvalidEntry()
    {
        var user = await SeedActiveDentistUserAsync("BSCKII. Trần Thị Lan Anh");
        _db.WorkSchedules.AddRange(
            WorkSchedule.Create(Today, "morning", "dentist", "dentist", user.FullName!, "Phòng 2", "border-secondary", false),
            WorkSchedule.Create(Today, "10:00-12:00", "dentist", "dentist", user.FullName!, "Phòng 2", "border-secondary", false));
        await _db.SaveChangesAsync();

        var result = await _handler.HandleAsync(Today);

        var dentist = result.Dentists.Should().ContainSingle().Subject;
        dentist.MorningSlots.Should().NotBeEmpty();
        dentist.AfternoonSlots.Should().BeEmpty();
    }

    [Test]
    public async Task HandleAsync_NoWorkSchedulesToday_ReturnsEmptyDentistList()
    {
        var result = await _handler.HandleAsync(Today);

        result.Dentists.Should().BeEmpty();
    }

    /// <summary>Với một ngày đã qua, toàn bộ slot trong ngày đó chắc chắn đã qua giờ.</summary>
    [Test]
    public async Task HandleAsync_PastDate_AllSlotsMarkedAsPast()
    {
        var yesterday = Today.AddDays(-1);
        var user = await SeedActiveDentistUserAsync("BS. Nguyễn Văn Hùng");
        _db.WorkSchedules.Add(WorkSchedule.Create(
            yesterday, "morning", "dentist", "dentist", user.FullName!, "Phòng 1", "border-primary", false));
        await _db.SaveChangesAsync();

        var result = await _handler.HandleAsync(yesterday);

        var dentist = result.Dentists.Should().ContainSingle().Subject;
        dentist.MorningSlots.Should().OnlyContain(s => s.IsPast);
    }

    /// <summary>Với một ngày trong tương lai, chưa có slot nào được coi là đã qua giờ.</summary>
    [Test]
    public async Task HandleAsync_FutureDate_NoSlotsMarkedAsPast()
    {
        var nextWeek = Today.AddDays(7);
        var user = await SeedActiveDentistUserAsync("BS. Nguyễn Văn Hùng");
        _db.WorkSchedules.Add(WorkSchedule.Create(
            nextWeek, "morning", "dentist", "dentist", user.FullName!, "Phòng 1", "border-primary", false));
        await _db.SaveChangesAsync();

        var result = await _handler.HandleAsync(nextWeek);

        var dentist = result.Dentists.Should().ContainSingle().Subject;
        dentist.MorningSlots.Should().OnlyContain(s => !s.IsPast);
    }
}
