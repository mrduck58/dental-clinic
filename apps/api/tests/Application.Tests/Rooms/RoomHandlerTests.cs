using DentalClinic.API.Application.DTOs.Rooms;
using DentalClinic.API.Application.UseCases.Rooms;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Rooms;

[TestFixture]
public class RoomHandlerTests
{
    private IRoomRepository _repo = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = Substitute.For<IRoomRepository>();
        _repo.ExistsByCodeAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(false);
        _repo.ExistsByNameAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(false);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CreateRoomHandler
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Tạo phòng với code và tên mới phải gọi AddAsync 1 lần và trả về RoomDto.
    /// </summary>
    [Test]
    public async Task Create_ValidRequest_CallsAddAsyncAndReturnsDto()
    {
        var handler = new CreateRoomHandler(_repo);

        var result = await handler.HandleAsync(BuildCreateRequest("P01", "Phòng 1"));

        await _repo.Received(1).AddAsync(Arg.Any<Room>(), Arg.Any<CancellationToken>());
        result.Should().NotBeNull();
        result.Name.Should().Be("Phòng 1");
    }

    /// <summary>
    /// Code phòng phải được tự động chuyển thành chữ hoa trong DTO trả về,
    /// đảm bảo nhất quán định dạng mã phòng trong toàn hệ thống.
    /// </summary>
    [Test]
    public async Task Create_LowercaseCode_ReturnedCodeIsUpperCase()
    {
        var handler = new CreateRoomHandler(_repo);

        var result = await handler.HandleAsync(BuildCreateRequest("p01", "Phòng 1"));

        result.Code.Should().Be("P01");
    }

    /// <summary>
    /// Code phòng đã tồn tại phải ném ConflictException trước khi gọi AddAsync.
    /// </summary>
    [Test]
    public async Task Create_DuplicateCode_ThrowsConflictException()
    {
        _repo.ExistsByCodeAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(true);
        var handler = new CreateRoomHandler(_repo);

        Func<Task> act = () => handler.HandleAsync(BuildCreateRequest("P01", "Phòng Mới"));

        await act.Should().ThrowAsync<ConflictException>();
    }

    /// <summary>
    /// Tên phòng đã tồn tại phải ném ConflictException.
    /// </summary>
    [Test]
    public async Task Create_DuplicateName_ThrowsConflictException()
    {
        _repo.ExistsByNameAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(true);
        var handler = new CreateRoomHandler(_repo);

        Func<Task> act = () => handler.HandleAsync(BuildCreateRequest("P99", "Phòng Trùng Tên"));

        await act.Should().ThrowAsync<ConflictException>();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // UpdateRoomHandler
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Cập nhật phòng tồn tại phải gọi UpdateAsync 1 lần và trả về DTO mới.
    /// </summary>
    [Test]
    public async Task Update_ExistingRoom_CallsUpdateAsyncOnce()
    {
        var room = MakeRoom();
        _repo.GetByIdAsync(room.Id, Arg.Any<CancellationToken>()).Returns(room);
        var handler = new UpdateRoomHandler(_repo);

        await handler.HandleAsync(room.Id, BuildUpdateRequest("P02", "Phòng Mới"));

        await _repo.Received(1).UpdateAsync(room, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Phòng không tồn tại phải ném NotFoundException.
    /// </summary>
    [Test]
    public async Task Update_NotFound_ThrowsNotFoundException()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Room?)null);
        var handler = new UpdateRoomHandler(_repo);

        Func<Task> act = () => handler.HandleAsync(Guid.NewGuid(), BuildUpdateRequest("P01", "Phòng"));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>
    /// Đổi sang code đã dùng bởi phòng khác phải ném ConflictException.
    /// ExcludeId được truyền vào để không conflict với chính phòng đang update.
    /// </summary>
    [Test]
    public async Task Update_DuplicateCode_ThrowsConflictException()
    {
        var room = MakeRoom("P01");
        _repo.GetByIdAsync(room.Id, Arg.Any<CancellationToken>()).Returns(room);
        _repo.ExistsByCodeAsync("P02", room.Id, Arg.Any<CancellationToken>()).Returns(true);
        var handler = new UpdateRoomHandler(_repo);

        Func<Task> act = () => handler.HandleAsync(room.Id, BuildUpdateRequest("P02", "Phòng 1"));

        await act.Should().ThrowAsync<ConflictException>();
    }

    /// <summary>
    /// Đổi sang tên đã dùng bởi phòng khác phải ném ConflictException.
    /// </summary>
    [Test]
    public async Task Update_DuplicateName_ThrowsConflictException()
    {
        var room = MakeRoom();
        _repo.GetByIdAsync(room.Id, Arg.Any<CancellationToken>()).Returns(room);
        _repo.ExistsByNameAsync("Phòng Trùng", room.Id, Arg.Any<CancellationToken>()).Returns(true);
        var handler = new UpdateRoomHandler(_repo);

        Func<Task> act = () => handler.HandleAsync(room.Id, BuildUpdateRequest("P01", "Phòng Trùng"));

        await act.Should().ThrowAsync<ConflictException>();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // DeleteRoomHandler
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Xóa phòng tồn tại phải gọi DeleteAsync 1 lần với đúng entity.
    /// </summary>
    [Test]
    public async Task Delete_ExistingRoom_CallsDeleteAsyncOnce()
    {
        var room = MakeRoom();
        _repo.GetByIdAsync(room.Id, Arg.Any<CancellationToken>()).Returns(room);
        var handler = new DeleteRoomHandler(_repo);

        await handler.HandleAsync(room.Id);

        await _repo.Received(1).DeleteAsync(room, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Xóa phòng không tồn tại phải ném NotFoundException, không gọi DeleteAsync.
    /// </summary>
    [Test]
    public async Task Delete_NotFound_ThrowsNotFoundException()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Room?)null);
        var handler = new DeleteRoomHandler(_repo);

        Func<Task> act = () => handler.HandleAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
        await _repo.DidNotReceive().DeleteAsync(Arg.Any<Room>(), Arg.Any<CancellationToken>());
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ChangeRoomStatusHandler
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Đổi trạng thái phòng hợp lệ phải gọi UpdateAsync và trả về DTO với trạng thái mới.
    /// </summary>
    [Test]
    public async Task ChangeStatus_ValidStatus_UpdatesAndReturnsDto()
    {
        var room = MakeRoom();
        _repo.GetByIdAsync(room.Id, Arg.Any<CancellationToken>()).Returns(room);
        var handler = new ChangeRoomStatusHandler(_repo);

        var result = await handler.HandleAsync(room.Id, new ChangeRoomStatusRequest("Đang khám"));

        await _repo.Received(1).UpdateAsync(room, Arg.Any<CancellationToken>());
        result.Status.Should().Be("Đang khám");
    }

    /// <summary>
    /// Đổi trạng thái phòng không tồn tại phải ném NotFoundException.
    /// </summary>
    [Test]
    public async Task ChangeStatus_NotFound_ThrowsNotFoundException()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Room?)null);
        var handler = new ChangeRoomStatusHandler(_repo);

        Func<Task> act = () => handler.HandleAsync(Guid.NewGuid(), new ChangeRoomStatusRequest("Trống"));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>
    /// Trạng thái không hợp lệ (không nằm trong danh sách tiếng Việt) phải ném ArgumentException.
    /// </summary>
    [Test]
    public async Task ChangeStatus_InvalidStatus_ThrowsArgumentException()
    {
        var room = MakeRoom();
        _repo.GetByIdAsync(room.Id, Arg.Any<CancellationToken>()).Returns(room);
        var handler = new ChangeRoomStatusHandler(_repo);

        Func<Task> act = () => handler.HandleAsync(room.Id, new ChangeRoomStatusRequest("InvalidStatus"));

        await act.Should().ThrowAsync<ArgumentException>();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetRoomsHandler
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Không có filter trả về toàn bộ danh sách phòng.
    /// </summary>
    [Test]
    public async Task GetRooms_NoFilters_ReturnsAllRooms()
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
    public async Task GetRooms_FilterByFloor_ReturnsOnlyMatchingFloor()
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
    /// Filter theo tên/code tìm kiếm không phân biệt hoa thường.
    /// Dùng loại phòng không chứa từ khóa tìm kiếm để test chỉ match theo tên.
    /// </summary>
    [Test]
    public async Task GetRooms_SearchByName_ReturnsMatchingRooms()
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
    /// (handler bắt ArgumentException và bỏ qua filter).
    /// </summary>
    [Test]
    public async Task GetRooms_InvalidStatusFilter_ReturnsAllRooms()
    {
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Room>
        {
            MakeRoom("P01"), MakeRoom("P02"),
        });
        var handler = new GetRoomsHandler(_repo);

        var result = await handler.HandleAsync(null, status: "KhongHopLe", null);

        result.Should().HaveCount(2);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetRoomByIdHandler
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Lấy phòng theo ID tồn tại phải trả về DTO với đầy đủ thông tin.
    /// </summary>
    [Test]
    public async Task GetById_ExistingRoom_ReturnsDto()
    {
        var room = MakeRoom("P01", name: "Phòng Test");
        _repo.GetByIdAsync(room.Id, Arg.Any<CancellationToken>()).Returns(room);
        var handler = new GetRoomByIdHandler(_repo);

        var result = await handler.HandleAsync(room.Id);

        result.Id.Should().Be(room.Id);
        result.Name.Should().Be("Phòng Test");
    }

    /// <summary>
    /// ID không tồn tại phải ném NotFoundException.
    /// </summary>
    [Test]
    public async Task GetById_NotFound_ThrowsNotFoundException()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Room?)null);
        var handler = new GetRoomByIdHandler(_repo);

        Func<Task> act = () => handler.HandleAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════════════════════

    private static Room MakeRoom(string code = "P01", string name = "Phòng Mặc Định", string floor = "1")
        => Room.Create(code, name, floor, "Phòng khám", "Mô tả");

    private static CreateRoomRequest BuildCreateRequest(string code, string name)
        => new(code, name, "1", "Phòng khám", "Mô tả phòng");

    private static UpdateRoomRequest BuildUpdateRequest(string code, string name)
        => new(code, name, "1", "Phòng khám", "Mô tả cập nhật");
}
