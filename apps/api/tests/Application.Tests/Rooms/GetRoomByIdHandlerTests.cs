using DentalClinic.API.Application.UseCases.Rooms;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Rooms;

[TestFixture]
public class GetRoomByIdHandlerTests
{
    private IRoomRepository _repo = null!;

    [SetUp]
    public void SetUp() => _repo = Substitute.For<IRoomRepository>();

    /// <summary>
    /// Lấy phòng theo ID tồn tại phải trả về DTO với đầy đủ thông tin.
    /// </summary>
    [Test]
    public async Task HandleAsync_ExistingRoom_ReturnsDto()
    {
        var room = Room.Create("P01", "Phòng Test", "1", "Loại", "Mô tả");
        _repo.GetByIdAsync(room.Id, Arg.Any<CancellationToken>()).Returns(room);
        var handler = new GetRoomByIdHandler(_repo);

        var result = await handler.Handle(new GetRoomByIdQuery(room.Id), CancellationToken.None);

        result.Id.Should().Be(room.Id);
        result.Name.Should().Be("Phòng Test");
    }

    /// <summary>
    /// ID không tồn tại phải ném NotFoundException.
    /// </summary>
    [Test]
    public async Task HandleAsync_NotFound_ThrowsNotFoundException()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Room?)null);
        var handler = new GetRoomByIdHandler(_repo);

        Func<Task> act = () => handler.Handle(new GetRoomByIdQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
