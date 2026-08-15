using DentalClinic.API.Application.DTOs.Schedules;
using DentalClinic.API.Application.UseCases.Schedules;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Interfaces.Repositories;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Schedules;

[TestFixture]
public class SaveWeekScheduleHandlerTests
{
    private IWorkScheduleRepository _repo = null!;
    private IUserRepository _userRepo = null!;

    // Cùng một instance được trả về mọi lần gọi, nên thêm vào đây là handler thấy ngay. Cấu hình
    // lại substitute sau mỗi lần seed thì phải gọi GetAllAsync ngoài ngữ cảnh Returns/Received —
    // NSubstitute để lại arg matcher lơ lửng và làm hỏng lần cấu hình kế tiếp.
    private readonly List<User> _users = [];

    [SetUp]
    public void SetUp()
    {
        _repo = Substitute.For<IWorkScheduleRepository>();
        _repo.ReplaceWeekAsync(Arg.Any<DateOnly>(), Arg.Any<IEnumerable<WorkSchedule>>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _users.Clear();
        _userRepo = Substitute.For<IUserRepository>();
        _userRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(_users);
    }

    private SaveWeekScheduleHandler NewHandler() => new(_repo, _userRepo);

    /// <summary>Nhân sự có hồ sơ để dò tên — trả về employeeId cho tiện assert.</summary>
    private Guid SeedEmployee(string fullName)
    {
        var user = User.CreateEmployee($"{Guid.NewGuid():N}@clinic.local", UserRole.Dentist, fullName: fullName);
        var employee = Employee.Create(user.Id, $"NV-{Guid.NewGuid().ToString("N")[..6].ToUpper()}");
        user.AttachEmployee(employee);
        _users.Add(user);
        return employee.Id;
    }

    /// <summary>
    /// Lưu lịch tuần hợp lệ phải gọi ReplaceWeekAsync 1 lần với weekStart đúng
    /// và trả về danh sách ScheduleEntryDto đã lưu.
    /// </summary>
    [Test]
    public async Task HandleAsync_ValidRequest_CallsReplaceWeekAndReturnsEntries()
    {
        var handler = NewHandler();
        var request = new SaveWeekScheduleRequest(new List<SaveScheduleEntryRequest>
        {
            new("2026-06-16", "Sáng", "Khám", "Nha sĩ", "Bs. An", "Phòng 1", "#FF0000", false),
            new("2026-06-17", "Chiều", "Phẫu thuật", "Bác sĩ", "Bs. Bình", "Phòng 2", "#00FF00", false),
        });

        var result = await handler.Handle(new SaveWeekScheduleCommand("2026-06-16", request), CancellationToken.None);

        await _repo.Received(1).ReplaceWeekAsync(
            new DateOnly(2026, 6, 16),
            Arg.Any<IEnumerable<WorkSchedule>>(),
            Arg.Any<CancellationToken>());
        result.Should().HaveCount(2);
    }

    /// <summary>
    /// weekStart không đúng định dạng phải ném ArgumentException trước khi xử lý entries.
    /// </summary>
    [Test]
    public async Task HandleAsync_InvalidWeekStartFormat_ThrowsArgumentException()
    {
        var handler = NewHandler();
        var request = new SaveWeekScheduleRequest(new List<SaveScheduleEntryRequest>());

        Func<Task> act = () => handler.Handle(new SaveWeekScheduleCommand("16-06-2026", request), CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
        await _repo.DidNotReceive().ReplaceWeekAsync(Arg.Any<DateOnly>(), Arg.Any<IEnumerable<WorkSchedule>>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Entry có date không đúng định dạng phải ném ArgumentException,
    /// ngay cả khi weekStart hợp lệ.
    /// </summary>
    [Test]
    public async Task HandleAsync_InvalidEntryDate_ThrowsArgumentException()
    {
        var handler = NewHandler();
        var request = new SaveWeekScheduleRequest(new List<SaveScheduleEntryRequest>
        {
            new("invalid-date", "Sáng", "Khám", "Nha sĩ", "Bs. An", "Phòng 1", "#FF0000", false),
        });

        Func<Task> act = () => handler.Handle(new SaveWeekScheduleCommand("2026-06-16", request), CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    /// <summary>
    /// Lưu lịch trống (không có entry) phải gọi ReplaceWeekAsync với danh sách rỗng
    /// (để xóa lịch cũ của tuần đó).
    /// </summary>
    [Test]
    public async Task HandleAsync_EmptyEntries_CallsReplaceWithEmptyList()
    {
        var handler = NewHandler();
        var request = new SaveWeekScheduleRequest(new List<SaveScheduleEntryRequest>());

        var result = await handler.Handle(new SaveWeekScheduleCommand("2026-06-16", request), CancellationToken.None);

        await _repo.Received(1).ReplaceWeekAsync(
            new DateOnly(2026, 6, 16),
            Arg.Is<IEnumerable<WorkSchedule>>(e => !e.Any()),
            Arg.Any<CancellationToken>());
        result.Should().BeEmpty();
    }

    /// <summary>Bắt lại danh sách đã lưu để soi EmployeeId từng dòng.</summary>
    private List<WorkSchedule> CapturedEntries() =>
        _repo.ReceivedCalls()
            .Single(c => c.GetMethodInfo().Name == nameof(IWorkScheduleRepository.ReplaceWeekAsync))
            .GetArguments()[1] is IEnumerable<WorkSchedule> rows ? rows.ToList() : [];

    private static SaveWeekScheduleRequest OneEntryFor(string name) =>
        new([new("2026-06-16", "08:00-10:00", "dentist", "dentist", name, "Phòng 1", "#FF0000", false)]);

    /// <summary>
    /// Điểm cốt lõi: dòng lịch phải được nối về nhân sự bằng KHÓA NGOẠI ngay lúc lưu. Trước đây
    /// EmployeeId luôn null nên tầng đọc phải dò lại bằng chuỗi tên, và chỉ lệch một tiền tố "BS."
    /// là bác sĩ biến mất khỏi lưới đặt lịch tại quầy.
    /// </summary>
    [Test]
    public async Task Save_LinksEntryToEmployee()
    {
        var employeeId = SeedEmployee("BS. Trần Văn A");

        await NewHandler().Handle(
            new SaveWeekScheduleCommand("2026-06-16", OneEntryFor("BS. Trần Văn A")), CancellationToken.None);

        CapturedEntries().Single().EmployeeId.Should().Be(employeeId);
    }

    /// <summary>
    /// Tên trong bảng xếp lịch hay thiếu chức danh so với hồ sơ (dữ liệu thật: hồ sơ ghi
    /// "BS.Đỗ Văn Phong", lịch ghi "Đỗ Văn Phong"). Vẫn phải nối đúng người.
    /// </summary>
    [Test]
    public async Task Save_LinksEmployee_EvenWhenTitlePrefixDiffers()
    {
        var employeeId = SeedEmployee("BS.Đỗ Văn Phong");

        await NewHandler().Handle(
            new SaveWeekScheduleCommand("2026-06-16", OneEntryFor("Đỗ Văn Phong")), CancellationToken.None);

        CapturedEntries().Single().EmployeeId.Should().Be(employeeId);
    }

    /// <summary>
    /// Tên không khớp ai (thường do gõ sai lúc import Excel) vẫn phải lưu được, chỉ là chưa nối
    /// được người. Chặn lại sẽ làm hỏng cả tuần lịch chỉ vì một ô sai chính tả.
    /// </summary>
    [Test]
    public async Task Save_UnknownName_IsStoredWithoutEmployee()
    {
        SeedEmployee("BS. Trần Văn A");

        await NewHandler().Handle(
            new SaveWeekScheduleCommand("2026-06-16", OneEntryFor("Người Lạ")), CancellationToken.None);

        var saved = CapturedEntries().Single();
        saved.EmployeeId.Should().BeNull();
        saved.StaffName.Should().Be("Người Lạ");
    }

    /// <summary>
    /// Hai nhân sự trùng tên sau khi bỏ chức danh thì KHÔNG đoán bừa: gán nhầm ca sang bác sĩ khác
    /// tệ hơn nhiều so với việc để trống rồi nhờ người xếp lịch chỉ định lại.
    /// </summary>
    [Test]
    public async Task Save_AmbiguousName_LeavesEmployeeUnset()
    {
        SeedEmployee("BS. Trần Văn A");
        SeedEmployee("Trần Văn A");

        await NewHandler().Handle(
            new SaveWeekScheduleCommand("2026-06-16", OneEntryFor("Trần Văn A")), CancellationToken.None);

        CapturedEntries().Single().EmployeeId.Should().BeNull();
    }
}
