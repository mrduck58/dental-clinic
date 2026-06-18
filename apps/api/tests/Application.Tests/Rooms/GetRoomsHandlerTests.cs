using DentalClinic.API.Application.UseCases.Rooms;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Rooms;

[TestFixture]
public class GetRoomsHandlerTests
{
    private IRoomRepository _repo = null!;

    [SetUp]
    public void SetUp() => _repo = Substitute.For<IRoomRepository>();

    /// <summary>
    /// Không có filter trả về toàn bộ danh sách phòng.
    /// </summary>
    [Test]
    public async Task HandleAsync_NoFilters_ReturnsAllRooms()
    {
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Room>
        {
            MakeRoom("P01", floor: "1"), MakeRoom("P02", floor: "2"), MakeRoom("P03", floor: "1"),
        });
        var handler = new GetRoomsHandler(_repo);

        var result = await handler.HandleAsync(null, null, null);

        result.Should().HaveCount(3);
    }

    /// <summary>
    /// Filter theo tầng chỉ trả về phòng ở tầng đó.
    /// </summary>
    [Test]
    public async Task HandleAsync_FilterByFloor_ReturnsOnlyMatchingFloor()
    {
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Room>
        {
            MakeRoom("P01", floor: "1"), MakeRoom("P02", floor: "2"), MakeRoom("P03", floor: "1"),
        });
        var handler = new GetRoomsHandler(_repo);

        var result = await handler.HandleAsync(floor: "1", null, null);

        result.Should().HaveCount(2);
        result.Should().OnlyContain(r => r.Floor == "1");
    }

    /// <summary>
    /// Filter search khớp theo tên phòng không phân biệt hoa thường.
    /// Dùng loại phòng không chứa từ khóa để chỉ test match theo tên.
    /// </summary>
    [Test]
    public async Task HandleAsync_SearchByName_ReturnsMatchingRooms()
    {
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Room>
        {
            Room.Create("P01", "Phòng Khám 1", "1", "Nội tổng hợp", "Mô tả"),
            Room.Create("P02", "Phòng Phẫu Thuật", "1", "Ngoại tổng hợp", "Mô tả"),
            Room.Create("P03", "Phòng Khám 2", "1", "Nội tổng hợp", "Mô tả"),
        });
        var handler = new GetRoomsHandler(_repo);

        var result = await handler.HandleAsync(null, null, search: "khám");

        result.Should().HaveCount(2);
    }

    /// <summary>
    /// Trạng thái filter không hợp lệ không throw exception mà trả về toàn bộ danh sách
    /// vì handler bắt ArgumentException và bỏ qua filter.
    /// </summary>
    [Test]
    public async Task HandleAsync_InvalidStatusFilter_ReturnsAllRooms()
    {
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Room>
        {
            MakeRoom("P01"), MakeRoom("P02"),
        });
        var handler = new GetRoomsHandler(_repo);

        var result = await handler.HandleAsync(null, status: "KhongHopLe", null);

        result.Should().HaveCount(2);
    }

    private static Room MakeRoom(string code, string name = "Phòng Test", string floor = "1")
        => Room.Create(code, name, floor, "Nội tổng hợp", "Mô tả");
}
