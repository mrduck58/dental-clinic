using DentalClinic.API.Application.DTOs.Schedules;
using DentalClinic.API.Application.UseCases.Schedules;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Schedules;

[TestFixture]
public class SaveWeekScheduleHandlerTests
{
    private IWorkScheduleRepository _repo = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = Substitute.For<IWorkScheduleRepository>();
        _repo.ReplaceWeekAsync(Arg.Any<DateOnly>(), Arg.Any<IEnumerable<WorkSchedule>>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
    }

    /// <summary>
    /// Lưu lịch tuần hợp lệ phải gọi ReplaceWeekAsync 1 lần với weekStart đúng
    /// và trả về danh sách ScheduleEntryDto đã lưu.
    /// </summary>
    [Test]
    public async Task HandleAsync_ValidRequest_CallsReplaceWeekAndReturnsEntries()
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
    public async Task HandleAsync_InvalidWeekStartFormat_ThrowsArgumentException()
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
    public async Task HandleAsync_InvalidEntryDate_ThrowsArgumentException()
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
    public async Task HandleAsync_EmptyEntries_CallsReplaceWithEmptyList()
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
}
