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
    /// Bác sĩ chỉ có dòng WorkSchedule với Shift không hợp lệ (không khớp bất kỳ mã ca nào —
    /// dữ liệu rác) không được coi là đang làm việc hôm nay, nên không được xuất hiện trong
    /// danh sách đặt lịch tại quầy.
    /// </summary>
    [Test]
    public async Task HandleAsync_DentistWithOnlyInvalidShiftValue_ExcludedFromResult()
    {
        var user = await SeedActiveDentistUserAsync("Dentist Test");
        _db.WorkSchedules.Add(WorkSchedule.Create(
            Today, "ca-khong-ton-tai", "dentist", "dentist", user.FullName!, "Phòng 2", "border-primary", false));
        await _db.SaveChangesAsync();

        var result = await _handler.HandleAsync(Today);

        result.Dentists.Should().BeEmpty();
    }

    /// <summary>
    /// Bác sĩ có ca làm việc hợp lệ theo mã ca 2 tiếng mới ("08:00-10:00") phải xuất hiện
    /// trong danh sách, với các slot nằm trong khung 2 tiếng đó — bao gồm cả mốc giờ kết thúc
    /// ca (10:00) vì mốc kết thúc vẫn được tính là thuộc ca.
    /// </summary>
    [Test]
    public async Task HandleAsync_DentistWithNewTwoHourShiftCode_IncludedWithSlotsInThatWindowOnly()
    {
        var user = await SeedActiveDentistUserAsync("BS. Nguyễn Văn Hùng");
        _db.WorkSchedules.Add(WorkSchedule.Create(
            Today, "08:00-10:00", "dentist", "dentist", user.FullName!, "Phòng 1", "border-primary", false));
        await _db.SaveChangesAsync();

        var result = await _handler.HandleAsync(Today);

        var dentist = result.Dentists.Should().ContainSingle().Subject;
        dentist.Name.Should().Be("BS. Nguyễn Văn Hùng");
        dentist.Slots.Should().NotBeEmpty();
        dentist.Slots.Should().OnlyContain(s => int.Parse(s.Time.Substring(0, 2)) < 12);
    }

    /// <summary>
    /// Khi một bác sĩ có cả dòng hợp lệ ("morning") lẫn dòng rác (không khớp mã ca nào) trong
    /// cùng ngày, dòng rác phải bị bỏ qua — bác sĩ vẫn chỉ hiện đúng slot buổi sáng.
    /// </summary>
    [Test]
    public async Task HandleAsync_DentistWithMixOfValidAndInvalidShift_IgnoresInvalidEntry()
    {
        var user = await SeedActiveDentistUserAsync("BSCKII. Trần Thị Lan Anh");
        _db.WorkSchedules.AddRange(
            WorkSchedule.Create(Today, "morning", "dentist", "dentist", user.FullName!, "Phòng 2", "border-secondary", false),
            WorkSchedule.Create(Today, "ca-khong-ton-tai", "dentist", "dentist", user.FullName!, "Phòng 2", "border-secondary", false));
        await _db.SaveChangesAsync();

        var result = await _handler.HandleAsync(Today);

        var dentist = result.Dentists.Should().ContainSingle().Subject;
        dentist.Slots.Should().NotBeEmpty();
        dentist.Slots.Should().OnlyContain(s => int.Parse(s.Time.Substring(0, 2)) < 12);
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
        dentist.Slots.Should().OnlyContain(s => s.IsPast);
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
        dentist.Slots.Should().OnlyContain(s => !s.IsPast);
    }
}
