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
public class CreateRoomHandlerTests
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
    /// Tạo phòng với code và tên mới phải gọi AddAsync 1 lần và trả về RoomDto.
    /// </summary>
    [Test]
    public async Task HandleAsync_ValidRequest_CallsAddAsyncAndReturnsDto()
    {
        var handler = new CreateRoomHandler(_repo, _activityLog, _currentUser);

        var result = await handler.HandleAsync(new CreateRoomRequest("P01", "Phòng 1", "1", "Phòng khám", "Mô tả"));

        await _repo.Received(1).AddAsync(Arg.Any<Room>(), Arg.Any<CancellationToken>());
        result.Name.Should().Be("Phòng 1");
    }

    /// <summary>
    /// Code phòng phải được tự động chuyển thành chữ hoa trong DTO trả về.
    /// </summary>
    [Test]
    public async Task HandleAsync_LowercaseCode_ReturnedCodeIsUpperCase()
    {
        var handler = new CreateRoomHandler(_repo, _activityLog, _currentUser);

        var result = await handler.HandleAsync(new CreateRoomRequest("p01", "Phòng 1", "1", "Phòng khám", "Mô tả"));

        result.Code.Should().Be("P01");
    }

    /// <summary>
    /// Code phòng đã tồn tại phải ném ConflictException trước khi gọi AddAsync.
    /// </summary>
    [Test]
    public async Task HandleAsync_DuplicateCode_ThrowsConflictException()
    {
        _repo.ExistsByCodeAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(true);
        var handler = new CreateRoomHandler(_repo, _activityLog, _currentUser);

        Func<Task> act = () => handler.HandleAsync(new CreateRoomRequest("P01", "Phòng Mới", "1", "Loại", "Mô tả"));

        await act.Should().ThrowAsync<ConflictException>();
    }

    /// <summary>
    /// Tên phòng đã tồn tại phải ném ConflictException.
    /// </summary>
    [Test]
    public async Task HandleAsync_DuplicateName_ThrowsConflictException()
    {
        _repo.ExistsByNameAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(true);
        var handler = new CreateRoomHandler(_repo, _activityLog, _currentUser);

        Func<Task> act = () => handler.HandleAsync(new CreateRoomRequest("P99", "Phòng Trùng Tên", "1", "Loại", "Mô tả"));

        await act.Should().ThrowAsync<ConflictException>();
    }

    /// <summary>
    /// Khi cả code và tên đều trùng, handler phải kiểm tra code trước và dừng lại ngay,
    /// không tiếp tục kiểm tra tên hay gọi AddAsync.
    /// </summary>
    [Test]
    public async Task HandleAsync_DuplicateCodeAndName_ChecksCodeFirstAndSkipsNameCheck()
    {
        _repo.ExistsByCodeAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(true);
        var handler = new CreateRoomHandler(_repo, _activityLog, _currentUser);

        Func<Task> act = () => handler.HandleAsync(new CreateRoomRequest("P01", "Phòng Trùng", "1", "Loại", "Mô tả"));

        await act.Should().ThrowAsync<ConflictException>();
        await _repo.DidNotReceive().ExistsByNameAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
        await _repo.DidNotReceive().AddAsync(Arg.Any<Room>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Tạo phòng thành công phải ghi log hoạt động (ActivityLogService) đúng 1 lần.
    /// </summary>
    [Test]
    public async Task HandleAsync_ValidRequest_LogsActivityOnce()
    {
        var handler = new CreateRoomHandler(_repo, _activityLog, _currentUser);

        await handler.HandleAsync(new CreateRoomRequest("P01", "Phòng 1", "1", "Phòng khám", "Mô tả"));

        await _activityLog.Received(1).LogAsync(
            userId: Arg.Any<Guid?>(), userName: Arg.Any<string>(), userRole: Arg.Any<string>(),
            action: Arg.Any<string>(), module: Arg.Any<string>(), description: Arg.Any<string>(),
            status: Arg.Any<string>(), ipAddress: Arg.Any<string?>(), targetId: Arg.Any<string?>(),
            ct: Arg.Any<CancellationToken>());
    }
}
