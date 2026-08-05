using DentalClinic.API.Application.UseCases.Booking;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
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

    private async Task<User> SeedActiveDentistUserAsync(string fullName, string employmentStatus = "Active")
    {
        var user = User.Create($"u-{Guid.NewGuid()}", $"{Guid.NewGuid()}@test.com", "hash", UserRole.Dentist, fullName: fullName);
        _db.Users.Add(user);
        var employee = Employee.Create(user.Id, $"DT-{Guid.NewGuid():N}", employmentStatus: employmentStatus);
        employee.User = user;
        user.AttachEmployee(employee);
        _db.Employees.Add(employee);
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

        var result = await _handler.Handle(new GetStaffScheduleQuery(Today), CancellationToken.None);

        result.Dentists.Should().BeEmpty();
    }

    /// <summary>
    /// Ca 2 tiếng là nửa khoảng [start, end) nên sinh đúng 4 khung giờ 30 phút; mốc kết thúc
    /// ca (10:00) KHÔNG thuộc ca vì lịch hẹn đặt tại đó sẽ tràn sang ca kế tiếp.
    /// </summary>
    [Test]
    public async Task HandleAsync_DentistWithNewTwoHourShiftCode_IncludedWithSlotsInThatWindowOnly()
    {
        var user = await SeedActiveDentistUserAsync("BS. Nguyễn Văn Hùng");
        _db.WorkSchedules.Add(WorkSchedule.Create(
            Today, "08:00-10:00", "dentist", "dentist", user.FullName!, "Phòng 1", "border-primary", false));
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetStaffScheduleQuery(Today), CancellationToken.None);

        var dentist = result.Dentists.Should().ContainSingle().Subject;
        dentist.Name.Should().Be("BS. Nguyễn Văn Hùng");
        dentist.Slots.Select(s => s.Time).Should().Equal("08:00", "08:30", "09:00", "09:30");
    }

    /// <summary>
    /// Mốc giao giữa hai ca liền kề chỉ thuộc ca đứng sau: bác sĩ được phân "10:00-12:00"
    /// bắt đầu từ 10:00, còn bác sĩ được phân "08:00-10:00" thì dừng trước 10:00.
    /// </summary>
    [Test]
    public async Task HandleAsync_ShiftBoundaryTime_BelongsOnlyToTheLaterShift()
    {
        var user = await SeedActiveDentistUserAsync("BS. Lê Minh Quân");
        _db.WorkSchedules.Add(WorkSchedule.Create(
            Today, "10:00-12:00", "dentist", "dentist", user.FullName!, "Phòng 3", "border-primary", false));
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetStaffScheduleQuery(Today), CancellationToken.None);

        var dentist = result.Dentists.Should().ContainSingle().Subject;
        dentist.Slots.Select(s => s.Time).Should().Equal("10:00", "10:30", "11:00", "11:30");
    }

    /// <summary>
    /// Hai ca liền nhau ghép lại phải cho đúng 8 khung giờ liên tục, không trùng lặp mốc giao.
    /// </summary>
    [Test]
    public async Task HandleAsync_TwoAdjacentShifts_ProduceEightContiguousSlots()
    {
        var user = await SeedActiveDentistUserAsync("BS. Phạm Thu Hà");
        _db.WorkSchedules.AddRange(
            WorkSchedule.Create(Today, "08:00-10:00", "dentist", "dentist", user.FullName!, "Phòng 4", "border-primary", false),
            WorkSchedule.Create(Today, "10:00-12:00", "dentist", "dentist", user.FullName!, "Phòng 4", "border-primary", false));
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetStaffScheduleQuery(Today), CancellationToken.None);

        var dentist = result.Dentists.Should().ContainSingle().Subject;
        dentist.Slots.Select(s => s.Time).Should()
            .Equal("08:00", "08:30", "09:00", "09:30", "10:00", "10:30", "11:00", "11:30");
    }

    /// <summary>
    /// Cột bác sĩ trên lưới xếp theo số phòng (1 → 2 → 3), không theo tên. Phòng không có số
    /// bị dồn xuống cuối để không chen ngang dãy phòng đánh số.
    /// </summary>
    [Test]
    public async Task HandleAsync_DentistsOrderedByRoomNumber_NotByName()
    {
        var a = await SeedActiveDentistUserAsync("BS. An");      // Phòng 3
        var b = await SeedActiveDentistUserAsync("BS. Bình");    // Phòng 1
        var c = await SeedActiveDentistUserAsync("BS. Cường");   // Phòng Test (không số)
        var d = await SeedActiveDentistUserAsync("BS. Dũng");    // Phòng 10
        _db.WorkSchedules.AddRange(
            WorkSchedule.Create(Today, "08:00-10:00", "dentist", "dentist", a.FullName!, "Phòng 3", "border-primary", false),
            WorkSchedule.Create(Today, "08:00-10:00", "dentist", "dentist", b.FullName!, "Phòng 1", "border-primary", false),
            WorkSchedule.Create(Today, "08:00-10:00", "dentist", "dentist", c.FullName!, "Phòng Test", "border-primary", false),
            WorkSchedule.Create(Today, "08:00-10:00", "dentist", "dentist", d.FullName!, "Phòng 10", "border-primary", false));
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetStaffScheduleQuery(Today), CancellationToken.None);

        result.Dentists.Select(x => x.Room).Should().Equal("Phòng 1", "Phòng 3", "Phòng 10", "Phòng Test");
    }

    /// <summary>Ca tối muộn nhất kết thúc 21:30 nên khung giờ cuối cùng đặt được là 21:00.</summary>
    [Test]
    public async Task HandleAsync_LastEveningShift_EndsAtNineOClockSlot()
    {
        var user = await SeedActiveDentistUserAsync("BS. Đỗ Văn Nam");
        _db.WorkSchedules.Add(WorkSchedule.Create(
            Today, "19:30-21:30", "dentist", "dentist", user.FullName!, "Phòng 5", "border-primary", false));
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetStaffScheduleQuery(Today), CancellationToken.None);

        var dentist = result.Dentists.Should().ContainSingle().Subject;
        dentist.Slots.Select(s => s.Time).Should().Equal("19:30", "20:00", "20:30", "21:00");
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

        var result = await _handler.Handle(new GetStaffScheduleQuery(Today), CancellationToken.None);

        var dentist = result.Dentists.Should().ContainSingle().Subject;
        dentist.Slots.Should().NotBeEmpty();
        dentist.Slots.Should().OnlyContain(s => int.Parse(s.Time.Substring(0, 2)) < 12);
    }

    [Test]
    public async Task HandleAsync_NoWorkSchedulesToday_ReturnsEmptyDentistList()
    {
        var result = await _handler.Handle(new GetStaffScheduleQuery(Today), CancellationToken.None);

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

        var result = await _handler.Handle(new GetStaffScheduleQuery(yesterday), CancellationToken.None);

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

        var result = await _handler.Handle(new GetStaffScheduleQuery(nextWeek), CancellationToken.None);

        var dentist = result.Dentists.Should().ContainSingle().Subject;
        dentist.Slots.Should().OnlyContain(s => !s.IsPast);
    }

    /// <summary>
    /// Khung giờ trùng đúng thời điểm một lịch hẹn đang tồn tại phải được đánh dấu IsBooked = true
    /// và trả về đúng tên bệnh nhân — đây là mục đích chính của lưới lịch cho lễ tân.
    /// </summary>
    [Test]
    public async Task HandleAsync_SlotMatchingExistingAppointment_MarksBookedWithPatientName()
    {
        var user = await SeedActiveDentistUserAsync("BS. Có lịch hẹn");
        _db.WorkSchedules.Add(WorkSchedule.Create(
            Today, "08:00-10:00", "dentist", "dentist", user.FullName!, "Phòng 1", "border-primary", false));

        var patientUser = User.Create("bn1", $"{Guid.NewGuid()}@test.com", "hash", UserRole.Patient, fullName: "Nguyễn Văn Bệnh Nhân");
        _db.Users.Add(patientUser);
        var patient = Patient.Create(patientUser.Id, new DateOnly(1990, 1, 1), "Nam");
        patient.User = patientUser;
        _db.Patients.Add(patient);
        var dentist = DentistProfile.Create(user.Employee!.Id, "Nha khoa tổng quát", "N/A", 5);
        dentist.Employee = user.Employee!;
        _db.DentistProfiles.Add(dentist);
        await _db.SaveChangesAsync();

        var appointmentDate = new DateTimeOffset(Today.Year, Today.Month, Today.Day, 8, 0, 0, TimeSpan.FromHours(7));
        _db.Appointments.Add(Appointment.Create(patient.Id, dentist.Id, appointmentDate));
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetStaffScheduleQuery(Today), CancellationToken.None);

        var dto = result.Dentists.Should().ContainSingle().Subject;
        var slot = dto.Slots.Single(s => s.Time == "08:00");
        slot.IsBooked.Should().BeTrue();
        slot.PatientName.Should().Be("Nguyễn Văn Bệnh Nhân");
        dto.Slots.Single(s => s.Time == "08:30").IsBooked.Should().BeFalse();
    }

    /// <summary>
    /// Lịch hẹn đã bị hủy (Cancelled) không được tính là chiếm slot — bệnh nhân khác vẫn phải thấy
    /// khung giờ đó còn trống để đặt lại.
    /// </summary>
    [Test]
    public async Task HandleAsync_CancelledAppointmentAtSlot_DoesNotMarkSlotAsBooked()
    {
        var user = await SeedActiveDentistUserAsync("BS. Có lịch hủy");
        _db.WorkSchedules.Add(WorkSchedule.Create(
            Today, "08:00-10:00", "dentist", "dentist", user.FullName!, "Phòng 1", "border-primary", false));
        var patientUser = User.Create("bn2", $"{Guid.NewGuid()}@test.com", "hash", UserRole.Patient, fullName: "Nguyễn Bệnh Nhân Hủy");
        _db.Users.Add(patientUser);
        var patient = Patient.Create(patientUser.Id, new DateOnly(1990, 1, 1), "Nam");
        patient.User = patientUser;
        _db.Patients.Add(patient);
        var dentist = DentistProfile.Create(user.Employee!.Id, "Nha khoa tổng quát", "N/A", 5);
        dentist.Employee = user.Employee!;
        _db.DentistProfiles.Add(dentist);
        await _db.SaveChangesAsync();

        var appointmentDate = new DateTimeOffset(Today.Year, Today.Month, Today.Day, 8, 0, 0, TimeSpan.FromHours(7));
        var appt = Appointment.Create(patient.Id, dentist.Id, appointmentDate);
        appt.Cancel("Bận việc");
        _db.Appointments.Add(appt);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetStaffScheduleQuery(Today), CancellationToken.None);

        var dto = result.Dentists.Should().ContainSingle().Subject;
        dto.Slots.Single(s => s.Time == "08:00").IsBooked.Should().BeFalse();
    }

    /// <summary>
    /// Bác sĩ có EmploymentStatus khác "Active" (đã nghỉ việc/tạm ngưng) không được xuất hiện trên
    /// lưới lịch dù vẫn còn WorkSchedule của ngày hôm đó (dữ liệu phân ca có thể chưa dọn kịp).
    /// </summary>
    [Test]
    public async Task HandleAsync_InactiveDentist_ExcludedFromResult()
    {
        var user = await SeedActiveDentistUserAsync("BS. Đã nghỉ việc", "Inactive");
        _db.WorkSchedules.Add(WorkSchedule.Create(
            Today, "08:00-10:00", "dentist", "dentist", user.FullName!, "Phòng 1", "border-primary", false));
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetStaffScheduleQuery(Today), CancellationToken.None);

        result.Dentists.Should().BeEmpty();
    }

    /// <summary>Vai trò "Doctor" (tên gọi khác của bác sĩ trong dữ liệu cũ) phải được chấp nhận
    /// tương đương "Dentist".</summary>
    [Test]
    public async Task HandleAsync_UserWithDoctorRole_IncludedInResult()
    {
        var user = User.Create($"u-{Guid.NewGuid()}", $"{Guid.NewGuid()}@test.com", "hash", UserRole.Dentist, fullName: "BS. Vai trò Doctor");
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        _db.WorkSchedules.Add(WorkSchedule.Create(
            Today, "08:00-10:00", "dentist", "dentist", user.FullName!, "Phòng 1", "border-primary", false));
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetStaffScheduleQuery(Today), CancellationToken.None);

        result.Dentists.Should().ContainSingle(d => d.Name == "BS. Vai trò Doctor");
    }

    /// <summary>Không truyền queryDate phải mặc định dùng ngày hôm nay theo giờ Việt Nam.</summary>
    [Test]
    public async Task HandleAsync_NullQueryDate_DefaultsToTodayInVietnamTimeZone()
    {
        var vietnamTz = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        var vietnamToday = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vietnamTz));
        var user = await SeedActiveDentistUserAsync("BS. Hôm nay");
        _db.WorkSchedules.Add(WorkSchedule.Create(
            vietnamToday, "08:00-10:00", "dentist", "dentist", user.FullName!, "Phòng 1", "border-primary", false));
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetStaffScheduleQuery(null), CancellationToken.None);

        result.Date.Should().Be(vietnamToday);
        result.Dentists.Should().ContainSingle(d => d.Name == "BS. Hôm nay");
    }
}
