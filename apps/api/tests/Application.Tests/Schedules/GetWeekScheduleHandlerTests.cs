using DentalClinic.API.Application.UseCases.Schedules;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Schedules;

[TestFixture]
public class GetWeekScheduleHandlerTests
{
    private IWorkScheduleRepository _repo = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = Substitute.For<IWorkScheduleRepository>();
        _repo.GetByWeekAsync(Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new List<WorkSchedule>());
    }

    /// <summary>
    /// Lấy lịch tuần với ngày hợp lệ (định dạng YYYY-MM-DD) phải gọi GetByWeekAsync
    /// và trả về danh sách ScheduleEntryDto.
    /// </summary>
    [Test]
    public async Task HandleAsync_ValidDate_CallsGetByWeekAndReturnsEntries()
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
    public async Task HandleAsync_InvalidDateFormat_ThrowsArgumentException()
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
    public async Task HandleAsync_NotADate_ThrowsArgumentException()
    {
        var handler = new GetWeekScheduleHandler(_repo);

        Func<Task> act = () => handler.HandleAsync("not-a-date", CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    /// <summary>
    /// Tuần trống (không có entry nào) phải trả về danh sách rỗng, không throw exception.
    /// </summary>
    [Test]
    public async Task HandleAsync_EmptyWeek_ReturnsEmptyList()
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
    public async Task HandleAsync_ValidEntry_MapsFieldsCorrectly()
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

    private static WorkSchedule MakeSchedule(
        DateOnly date,
        string shift = "Sáng",
        string type = "Khám",
        string role = "Nha sĩ",
        string staffName = "Bs. Test")
        => WorkSchedule.Create(date, shift, type, role, staffName, "Phòng 1", "#FFFFFF", false);
}
