using DentalClinic.API.Application.UseCases.Staff;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Staff;

[TestFixture]
public class GetStaffHandlerTests
{
    private IUserRepository _userRepo = null!;

    [SetUp]
    public void SetUp()
    {
        _userRepo = Substitute.For<IUserRepository>();
        _userRepo.GetStaffPagedAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((new List<User>().AsReadOnly(), 0));
        _userRepo.GetStaffStatsAsync(Arg.Any<CancellationToken>())
            .Returns(new StaffStatsResult(0, 0, 0));
    }

    /// <summary>
    /// Query page &lt; 1 phải được clamp về 1 để tránh lỗi SQL offset âm.
    /// </summary>
    [Test]
    public async Task HandleAsync_PageLessThanOne_ClampsToOne()
    {
        var handler = new GetStaffHandler(_userRepo);

        var result = await handler.HandleAsync(new GetStaffQuery(null, null, null, Page: 0, PageSize: 10));

        result.Page.Should().Be(1);
    }

    /// <summary>
    /// PageSize lớn hơn 100 phải được clamp về 100, tránh query trả về quá nhiều dòng.
    /// </summary>
    [Test]
    public async Task HandleAsync_PageSizeOver100_ClampedTo100()
    {
        var handler = new GetStaffHandler(_userRepo);

        await handler.HandleAsync(new GetStaffQuery(null, null, null, Page: 1, PageSize: 999));

        await _userRepo.Received(1).GetStaffPagedAsync(
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            1, 100, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// TotalCount và Items phải được lấy đúng từ kết quả paged query của repository.
    /// </summary>
    [Test]
    public async Task HandleAsync_ValidQuery_ReturnsTotalCountAndItems()
    {
        var users = new List<User>
        {
            User.Create("u1", "a@test.com", "h", "Staff"),
            User.Create("u2", "b@test.com", "h", "Dentist"),
        };
        _userRepo.GetStaffPagedAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((users.AsReadOnly(), 20));
        _userRepo.GetStaffStatsAsync(Arg.Any<CancellationToken>())
            .Returns(new StaffStatsResult(20, 5, 3));
        var handler = new GetStaffHandler(_userRepo);

        var result = await handler.HandleAsync(new GetStaffQuery(null, null, null, 1, 10));

        result.TotalCount.Should().Be(20);
        result.Items.Should().HaveCount(2);
    }

    /// <summary>
    /// Statistics trong kết quả phải ánh xạ đúng từ StaffStatsResult của repository.
    /// </summary>
    [Test]
    public async Task HandleAsync_ReturnsStatsFromRepo()
    {
        _userRepo.GetStaffStatsAsync(Arg.Any<CancellationToken>())
            .Returns(new StaffStatsResult(10, 3, 2));
        var handler = new GetStaffHandler(_userRepo);

        var result = await handler.HandleAsync(new GetStaffQuery(null, null, null, 1, 10));

        result.Statistics.TotalEmployees.Should().Be(10);
        result.Statistics.TotalDentists.Should().Be(3);
        result.Statistics.TotalDoctors.Should().Be(2);
    }
}
