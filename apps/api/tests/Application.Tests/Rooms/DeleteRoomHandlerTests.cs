using DentalClinic.API.Application.UseCases.Rooms;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Rooms;

[TestFixture]
public class DeleteRoomHandlerTests
{
    private IRoomRepository _repo = null!;

    [SetUp]
    public void SetUp() => _repo = Substitute.For<IRoomRepository>();

    /// <summary>
    /// Xóa phòng tồn tại phải gọi DeleteAsync 1 lần với đúng entity.
    /// </summary>
    [Test]
    public async Task HandleAsync_ExistingRoom_CallsDeleteAsyncOnce()
    {
        var room = Room.Create("P01", "Phòng 1", "1", "Loại", "Mô tả");
        _repo.GetByIdAsync(room.Id, Arg.Any<CancellationToken>()).Returns(room);
        var handler = new DeleteRoomHandler(_repo);

        await handler.HandleAsync(room.Id);

        await _repo.Received(1).DeleteAsync(room, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Xóa phòng không tồn tại phải ném NotFoundException, không gọi DeleteAsync.
    /// </summary>
    [Test]
    public async Task HandleAsync_NotFound_ThrowsNotFoundException()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Room?)null);
        var handler = new DeleteRoomHandler(_repo);

        Func<Task> act = () => handler.HandleAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
        await _repo.DidNotReceive().DeleteAsync(Arg.Any<Room>(), Arg.Any<CancellationToken>());
    }
}
