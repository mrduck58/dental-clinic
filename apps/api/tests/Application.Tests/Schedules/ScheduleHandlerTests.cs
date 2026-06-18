using DentalClinic.API.Application.DTOs.Schedules;
using DentalClinic.API.Application.UseCases.Schedules;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Schedules;

[TestFixture]
public class ScheduleHandlerTests
{
    private IWorkScheduleRepository _repo = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = Substitute.For<IWorkScheduleRepository>();
        _repo.GetByWeekAsync(Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new List<WorkSchedule>());
        _repo.ReplaceWeekAsync(Arg.Any<DateOnly>(), Arg.Any<IEnumerable<WorkSchedule>>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetWeekScheduleHandler
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Lấy lịch tuần với ngày hợp lệ (định dạng YYYY-MM-DD) phải gọi GetByWeekAsync
    /// và trả về danh sách ScheduleEntryDto.
    /// </summary>
    [Test]
    public async Task GetWeekSchedule_ValidDate_CallsGetByWeekAndReturnsEntries()
    {
        _repo.GetByWeekAsync(new DateOnly(2026, 6, 16), Arg.Any<CancellationToken>())
            .Returns(new List<WorkSchedule>
            {
                MakeSchedule(new DateOnly(2026, 6, 16)),
                MakeSchedule(new DateOnly(2026, 6, 17)),
            });
        var handler = new GetWeekScheduleHandler(_repo);

        var result = await handler.HandleAsync("2026-06-16", CancellationToken.None);

        result.Should().HaveCount(2);
        await _repo.Received(1).GetByWeekAsync(new DateOnly(2026, 6, 16), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Ngày không đúng định dạng YYYY-MM-DD phải ném ArgumentException ngay lập tức,
    /// không gọi repository.
    /// </summary>
    [Test]
    public async Task GetWeekSchedule_InvalidDateFormat_ThrowsArgumentException()
    {
        var handler = new GetWeekScheduleHandler(_repo);

        Func<Task> act = () => handler.HandleAsync("16/06/2026", CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
        await _repo.DidNotReceive().GetByWeekAsync(Arg.Any<DateOnly>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Chuỗi ngày không phải ngày hợp lệ phải ném ArgumentException.
    /// </summary>
    [Test]
    public async Task GetWeekSchedule_NotADate_ThrowsArgumentException()
    {
        var handler = new GetWeekScheduleHandler(_repo);

        Func<Task> act = () => handler.HandleAsync("not-a-date", CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    /// <summary>
    /// Tuần trống (không có entry nào) phải trả về danh sách rỗng, không throw exception.
    /// </summary>
    [Test]
    public async Task GetWeekSchedule_EmptyWeek_ReturnsEmptyList()
    {
        _repo.GetByWeekAsync(Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new List<WorkSchedule>());
        var handler = new GetWeekScheduleHandler(_repo);

        var result = await handler.HandleAsync("2026-06-16", CancellationToken.None);

        result.Should().BeEmpty();
    }

    /// <summary>
    /// DTO trả về phải ánh xạ đúng các trường từ WorkSchedule entity.
    /// </summary>
    [Test]
    public async Task GetWeekSchedule_ValidEntry_MapsFieldsCorrectly()
    {
        var schedule = MakeSchedule(new DateOnly(2026, 6, 16), "Sáng", "Khám", "Nha sĩ", "Bs. Nguyễn");
        _repo.GetByWeekAsync(Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new List<WorkSchedule> { schedule });
        var handler = new GetWeekScheduleHandler(_repo);

        var result = (await handler.HandleAsync("2026-06-16", CancellationToken.None)).ToList();

        result[0].Date.Should().Be("2026-06-16");
        result[0].Shift.Should().Be("Sáng");
        result[0].Type.Should().Be("Khám");
        result[0].Role.Should().Be("Nha sĩ");
        result[0].Name.Should().Be("Bs. Nguyễn");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SaveWeekScheduleHandler
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Lưu lịch tuần hợp lệ phải gọi ReplaceWeekAsync 1 lần với weekStart đúng
    /// và trả về danh sách ScheduleEntryDto đã lưu.
    /// </summary>
    [Test]
    public async Task SaveWeekSchedule_ValidRequest_CallsReplaceWeekAndReturnsEntries()
    {
        var handler = new SaveWeekScheduleHandler(_repo);
        var request = new SaveWeekScheduleRequest(new List<SaveScheduleEntryRequest>
        {
            new("2026-06-16", "Sáng", "Khám", "Nha sĩ", "Bs. An", "Phòng 1", "#FF0000", false),
            new("2026-06-17", "Chiều", "Phẫu thuật", "Bác sĩ", "Bs. Bình", "Phòng 2", "#00FF00", false),
        });

        var result = await handler.HandleAsync("2026-06-16", request, CancellationToken.None);

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
    public async Task SaveWeekSchedule_InvalidWeekStartFormat_ThrowsArgumentException()
    {
        var handler = new SaveWeekScheduleHandler(_repo);
        var request = new SaveWeekScheduleRequest(new List<SaveScheduleEntryRequest>());

        Func<Task> act = () => handler.HandleAsync("16-06-2026", request, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
        await _repo.DidNotReceive().ReplaceWeekAsync(Arg.Any<DateOnly>(), Arg.Any<IEnumerable<WorkSchedule>>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Entry có date không đúng định dạng phải ném ArgumentException,
    /// ngay cả khi weekStart hợp lệ.
    /// </summary>
    [Test]
    public async Task SaveWeekSchedule_InvalidEntryDate_ThrowsArgumentException()
    {
        var handler = new SaveWeekScheduleHandler(_repo);
        var request = new SaveWeekScheduleRequest(new List<SaveScheduleEntryRequest>
        {
            new("invalid-date", "Sáng", "Khám", "Nha sĩ", "Bs. An", "Phòng 1", "#FF0000", false),
        });

        Func<Task> act = () => handler.HandleAsync("2026-06-16", request, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    /// <summary>
    /// Lưu lịch trống (không có entry) phải gọi ReplaceWeekAsync với danh sách rỗng
    /// (để xóa lịch cũ của tuần đó).
    /// </summary>
    [Test]
    public async Task SaveWeekSchedule_EmptyEntries_CallsReplaceWithEmptyList()
    {
        var handler = new SaveWeekScheduleHandler(_repo);
        var request = new SaveWeekScheduleRequest(new List<SaveScheduleEntryRequest>());

        var result = await handler.HandleAsync("2026-06-16", request, CancellationToken.None);

        await _repo.Received(1).ReplaceWeekAsync(
            new DateOnly(2026, 6, 16),
            Arg.Is<IEnumerable<WorkSchedule>>(e => !e.Any()),
            Arg.Any<CancellationToken>());
        result.Should().BeEmpty();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════════════════════

    private static WorkSchedule MakeSchedule(
        DateOnly date,
        string shift = "Sáng",
        string type = "Khám",
        string role = "Nha sĩ",
        string staffName = "Bs. Test")
        => WorkSchedule.Create(date, shift, type, role, staffName, "Phòng 1", "#FFFFFF", false);
}
