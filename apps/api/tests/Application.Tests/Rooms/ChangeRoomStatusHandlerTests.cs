using DentalClinic.API.Application.DTOs.Rooms;
using DentalClinic.API.Application.UseCases.Rooms;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Rooms;

[TestFixture]
public class ChangeRoomStatusHandlerTests
{
    private IRoomRepository _repo = null!;

    [SetUp]
    public void SetUp() => _repo = Substitute.For<IRoomRepository>();

    /// <summary>
    /// Đổi trạng thái phòng hợp lệ phải gọi UpdateAsync và trả về DTO với trạng thái mới.
    /// </summary>
    [Test]
    public async Task HandleAsync_ValidStatus_UpdatesAndReturnsDto()
    {
        var room = Room.Create("P01", "Phòng 1", "1", "Loại", "Mô tả");
        _repo.GetByIdAsync(room.Id, Arg.Any<CancellationToken>()).Returns(room);
        var handler = new ChangeRoomStatusHandler(_repo);

        var result = await handler.HandleAsync(room.Id, new ChangeRoomStatusRequest("Đang khám"));

        await _repo.Received(1).UpdateAsync(room, Arg.Any<CancellationToken>());
        result.Status.Should().Be("Đang khám");
    }

    /// <summary>
    /// Phòng không tồn tại phải ném NotFoundException.
    /// </summary>
    [Test]
    public async Task HandleAsync_NotFound_ThrowsNotFoundException()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Room?)null);
        var handler = new ChangeRoomStatusHandler(_repo);

        Func<Task> act = () => handler.HandleAsync(Guid.NewGuid(), new ChangeRoomStatusRequest("Trống"));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>
    /// Trạng thái không hợp lệ phải ném ArgumentException.
    /// </summary>
    [Test]
    public async Task HandleAsync_InvalidStatus_ThrowsArgumentException()
    {
        var room = Room.Create("P01", "Phòng 1", "1", "Loại", "Mô tả");
        _repo.GetByIdAsync(room.Id, Arg.Any<CancellationToken>()).Returns(room);
        var handler = new ChangeRoomStatusHandler(_repo);

        Func<Task> act = () => handler.HandleAsync(room.Id, new ChangeRoomStatusRequest("KhongHopLe"));

        await act.Should().ThrowAsync<ArgumentException>();
    }

    /// <summary>
    /// Đổi trạng thái phòng sang "Ngừng hoạt động" phải phản ánh đúng ActiveStatus = "Ngừng hoạt động"
    /// trong DTO trả về, khác với các trạng thái còn lại (đều là "Hoạt động").
    /// </summary>
    [Test]
    public async Task HandleAsync_ChangeToNgungHoatDong_ReturnsInactiveActiveStatus()
    {
        var room = Room.Create("P01", "Phòng 1", "1", "Loại", "Mô tả");
        _repo.GetByIdAsync(room.Id, Arg.Any<CancellationToken>()).Returns(room);
        var handler = new ChangeRoomStatusHandler(_repo);

        var result = await handler.HandleAsync(room.Id, new ChangeRoomStatusRequest("Ngừng hoạt động"));

        result.Status.Should().Be("Ngừng hoạt động");
        result.ActiveStatus.Should().Be("Ngừng hoạt động");
    }

    /// <summary>
    /// Chuỗi trạng thái rỗng cũng phải ném ArgumentException vì không khớp giá trị Vietnamese hợp lệ nào.
    /// </summary>
    [Test]
    public async Task HandleAsync_EmptyStatus_ThrowsArgumentException()
    {
        var room = Room.Create("P01", "Phòng 1", "1", "Loại", "Mô tả");
        _repo.GetByIdAsync(room.Id, Arg.Any<CancellationToken>()).Returns(room);
        var handler = new ChangeRoomStatusHandler(_repo);

        Func<Task> act = () => handler.HandleAsync(room.Id, new ChangeRoomStatusRequest(""));

        await act.Should().ThrowAsync<ArgumentException>();
    }
}
