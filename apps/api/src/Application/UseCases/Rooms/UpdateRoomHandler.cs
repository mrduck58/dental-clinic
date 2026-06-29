using DentalClinic.API.Application.DTOs.Rooms;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Domain.Constants;

namespace DentalClinic.API.Application.UseCases.Rooms;

public class UpdateRoomHandler(
    IRoomRepository roomRepository,
    IActivityLogService activityLogService,
    ICurrentUserService currentUser)
{
    public async Task<RoomDto> HandleAsync(Guid id, UpdateRoomRequest request, CancellationToken ct = default)
    {
        var room = await roomRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Không tìm thấy phòng với ID: {id}");

        if (await roomRepository.ExistsByCodeAsync(request.Code, excludeId: id, ct: ct))
            throw new ConflictException($"Mã phòng '{request.Code.ToUpperInvariant()}' đã được sử dụng.");

        if (await roomRepository.ExistsByNameAsync(request.Name, excludeId: id, ct: ct))
            throw new ConflictException($"Tên phòng '{request.Name}' đã được sử dụng.");

        room.Update(request.Code, request.Name, request.Floor, request.Type, request.Description);
        await roomRepository.UpdateAsync(room, ct);

        await activityLogService.LogAsync(
            userId: currentUser.UserId,
            userName: currentUser.UserName,
            userRole: currentUser.UserRole,
            action: ActivityAction.Edit,
            module: ActivityModule.Room,
            description: $"Cập nhật phòng: {room.Name}",
            status: ActivityStatus.Success,
            ipAddress: currentUser.IpAddress,
            targetId: id.ToString(),
            ct: ct);

        return new RoomDto(
            room.Id, room.Code, room.Name, room.Floor, room.Type,
            room.Status.ToVietnamese(), room.Status.ToActiveStatus(),
            room.Description, room.CreatedAt, room.UpdatedAt);
    }
}
