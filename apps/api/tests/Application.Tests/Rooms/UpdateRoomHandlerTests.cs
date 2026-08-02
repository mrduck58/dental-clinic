using DentalClinic.API.Application.DTOs.Rooms;
using DentalClinic.API.Application.UseCases.Rooms;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Rooms;

[TestFixture]
public class UpdateRoomHandlerTests
{
    private IRoomRepository _repo = null!;
    private IActivityLogService _activityLog = null!;
    private ICurrentUserService _currentUser = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = Substitute.For<IRoomRepository>();
        _activityLog = Substitute.For<IActivityLogService>();
        _currentUser = Substitute.For<ICurrentUserService>();
        _repo.ExistsByCodeAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(false);
        _repo.ExistsByNameAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(false);
    }

    /// <summary>
    /// Cập nhật phòng tồn tại phải gọi UpdateAsync 1 lần.
    /// </summary>
    [Test]
    public async Task HandleAsync_ExistingRoom_CallsUpdateAsyncOnce()
    {
        var room = Room.Create("P01", "Phòng 1", "1", "Loại", "Mô tả");
        _repo.GetByIdAsync(room.Id, Arg.Any<CancellationToken>()).Returns(room);
        var handler = new UpdateRoomHandler(_repo, _activityLog, _currentUser);

        await handler.Handle(new UpdateRoomCommand(room.Id, new UpdateRoomRequest("P02", "Phòng Mới", "1", "Loại", "Mô tả")), CancellationToken.None);

        await _repo.Received(1).UpdateAsync(room, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Phòng không tồn tại phải ném NotFoundException.
    /// </summary>
    [Test]
    public async Task HandleAsync_NotFound_ThrowsNotFoundException()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Room?)null);
        var handler = new UpdateRoomHandler(_repo, _activityLog, _currentUser);

        Func<Task> act = () => handler.Handle(new UpdateRoomCommand(Guid.NewGuid(), new UpdateRoomRequest("P01", "Phòng", "1", "Loại", "Mô tả")), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>
    /// Đổi sang code đã dùng bởi phòng khác phải ném ConflictException.
    /// ExcludeId được truyền vào để không conflict với chính phòng đang update.
    /// </summary>
    [Test]
    public async Task HandleAsync_DuplicateCode_ThrowsConflictException()
    {
        var room = Room.Create("P01", "Phòng 1", "1", "Loại", "Mô tả");
        _repo.GetByIdAsync(room.Id, Arg.Any<CancellationToken>()).Returns(room);
        _repo.ExistsByCodeAsync("P02", room.Id, Arg.Any<CancellationToken>()).Returns(true);
        var handler = new UpdateRoomHandler(_repo, _activityLog, _currentUser);

        Func<Task> act = () => handler.Handle(new UpdateRoomCommand(room.Id, new UpdateRoomRequest("P02", "Phòng 1", "1", "Loại", "Mô tả")), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    /// <summary>
    /// Đổi sang tên đã dùng bởi phòng khác phải ném ConflictException.
    /// </summary>
    [Test]
    public async Task HandleAsync_DuplicateName_ThrowsConflictException()
    {
        var room = Room.Create("P01", "Phòng 1", "1", "Loại", "Mô tả");
        _repo.GetByIdAsync(room.Id, Arg.Any<CancellationToken>()).Returns(room);
        _repo.ExistsByNameAsync("Phòng Trùng", room.Id, Arg.Any<CancellationToken>()).Returns(true);
        var handler = new UpdateRoomHandler(_repo, _activityLog, _currentUser);

        Func<Task> act = () => handler.Handle(new UpdateRoomCommand(room.Id, new UpdateRoomRequest("P01", "Phòng Trùng", "1", "Loại", "Mô tả")), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    /// <summary>
    /// Giữ nguyên code/tên cũ (không đổi) không được coi là trùng lặp vì ExcludeId
    /// loại trừ chính phòng đang cập nhật khỏi kiểm tra tồn tại.
    /// </summary>
    [Test]
    public async Task HandleAsync_SameCodeAndName_DoesNotThrowConflict()
    {
        var room = Room.Create("P01", "Phòng 1", "1", "Loại", "Mô tả");
        _repo.GetByIdAsync(room.Id, Arg.Any<CancellationToken>()).Returns(room);
        var handler = new UpdateRoomHandler(_repo, _activityLog, _currentUser);

        Func<Task> act = () => handler.Handle(new UpdateRoomCommand(room.Id, new UpdateRoomRequest("P01", "Phòng 1", "2", "Loại Mới", "Mô tả mới")), CancellationToken.None);

        await act.Should().NotThrowAsync();
        await _repo.Received(1).UpdateAsync(room, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Phòng không tồn tại phải dừng ngay ở bước tìm kiếm, không được gọi tiếp
    /// ExistsByCodeAsync/ExistsByNameAsync hay UpdateAsync.
    /// </summary>
    [Test]
    public async Task HandleAsync_NotFound_DoesNotCheckDuplicatesOrUpdate()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Room?)null);
        var handler = new UpdateRoomHandler(_repo, _activityLog, _currentUser);

        Func<Task> act = () => handler.Handle(new UpdateRoomCommand(Guid.NewGuid(), new UpdateRoomRequest("P01", "Phòng", "1", "Loại", "Mô tả")), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await _repo.DidNotReceive().ExistsByCodeAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
        await _repo.DidNotReceive().ExistsByNameAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
        await _repo.DidNotReceive().UpdateAsync(Arg.Any<Room>(), Arg.Any<CancellationToken>());
    }
}
