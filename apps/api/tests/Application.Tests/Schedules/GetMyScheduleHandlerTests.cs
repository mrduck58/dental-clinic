using DentalClinic.API.Application.UseCases.Schedules;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Interfaces.Repositories;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Schedules;

[TestFixture]
public class GetMyScheduleHandlerTests
{
    private IDentistRepository _dentistRepo = null!;
    private IWorkScheduleRepository _workScheduleRepo = null!;
    private GetMyScheduleHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _dentistRepo = Substitute.For<IDentistRepository>();
        _workScheduleRepo = Substitute.For<IWorkScheduleRepository>();
        _handler = new GetMyScheduleHandler(_dentistRepo, _workScheduleRepo);
    }

    private static DentistProfile MakeDentist(string fullName = "Bs. Nguyễn")
    {
        var user = User.Create($"d-{Guid.NewGuid()}", $"{Guid.NewGuid()}@test.com", "hash", UserRole.Dentist, fullName: fullName);
        var employee = Employee.Create(user.Id, $"DT-{Guid.NewGuid():N}");
        employee.User = user;
        var dentist = DentistProfile.Create(employee.Id, "Nha khoa tổng quát", "N/A", 5);
        dentist.Employee = employee;
        return dentist;
    }

    private static WorkSchedule MakeSchedule(DateOnly date, string staffName)
        => WorkSchedule.Create(date, "Sáng", "Khám", "Nha sĩ", staffName, "Phòng 1", "#FFFFFF", false);

    /// <summary>Định dạng ngày không hợp lệ phải ném ArgumentException, không gọi repository.</summary>
    [Test]
    public async Task HandleAsync_InvalidDateFormat_ThrowsArgumentException()
    {
        Func<Task> act = () => _handler.Handle(new GetMyScheduleQuery(Guid.NewGuid(), "16/06/2026"), CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
        await _dentistRepo.DidNotReceive().GetByUserIdWithUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Tài khoản gọi không phải hồ sơ nha sĩ nào (dentist == null) phải trả về danh sách rỗng,
    /// không ném lỗi.</summary>
    [Test]
    public async Task HandleAsync_UserIsNotADentist_ReturnsEmptyList()
    {
        _dentistRepo.GetByUserIdWithUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((DentistProfile?)null);

        var result = await _handler.Handle(new GetMyScheduleQuery(Guid.NewGuid(), "2026-06-16"), CancellationToken.None);

        result.Should().BeEmpty();
        await _workScheduleRepo.DidNotReceive().GetByStaffNameAndDateRangeAsync(
            Arg.Any<string>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Phải lấy lịch theo đúng tên nha sĩ và đúng khoảng 7 ngày kể từ WeekStart, trả về đủ entry.</summary>
    [Test]
    public async Task HandleAsync_ValidDentistAndWeek_ReturnsMappedEntries()
    {
        var userId = Guid.NewGuid();
        var dentist = MakeDentist("Bs. Trần");
        var weekStart = new DateOnly(2026, 6, 16);
        _dentistRepo.GetByUserIdWithUserAsync(userId, Arg.Any<CancellationToken>()).Returns(dentist);
        _workScheduleRepo.GetByStaffNameAndDateRangeAsync("Bs. Trần", weekStart, weekStart.AddDays(7), Arg.Any<CancellationToken>())
            .Returns(new List<WorkSchedule>
            {
                MakeSchedule(weekStart, "Bs. Trần"),
                MakeSchedule(weekStart.AddDays(1), "Bs. Trần"),
            });

        var result = (await _handler.Handle(new GetMyScheduleQuery(userId, "2026-06-16"), CancellationToken.None)).ToList();

        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Bs. Trần");
        await _workScheduleRepo.Received(1).GetByStaffNameAndDateRangeAsync(
            "Bs. Trần", weekStart, weekStart.AddDays(7), Arg.Any<CancellationToken>());
    }
}
