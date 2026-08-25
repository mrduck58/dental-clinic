using DentalClinic.API.Application.DTOs.Rooms;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Domain.Constants;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Rooms;

public record UpdateRoomCommand(Guid Id, UpdateRoomRequest Request) : IRequest<RoomDto>;

public class UpdateRoomHandler(
    IRoomRepository roomRepository,
    IActivityLogService activityLogService,
    ICurrentUserService currentUser) : IRequestHandler<UpdateRoomCommand, RoomDto>
{
    public async Task<RoomDto> Handle(UpdateRoomCommand command, CancellationToken ct)
    {
        var request = command.Request;

        var room = await roomRepository.GetByIdAsync(command.Id, ct)
            ?? throw new NotFoundException($"Không tìm thấy phòng với ID: {command.Id}");

        if (await roomRepository.ExistsByCodeAsync(request.Code, excludeId: command.Id, ct: ct))
            throw new ConflictException($"Mã phòng '{request.Code.ToUpperInvariant()}' đã được sử dụng.");

        if (await roomRepository.ExistsByNameAsync(request.Name, excludeId: command.Id, ct: ct))
            throw new ConflictException($"Tên phòng '{request.Name}' đã được sử dụng.");

        room.Update(request.Code, request.Name, request.Floor, request.Description);
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
            targetId: command.Id.ToString(),
            ct: ct);

        return new RoomDto(
            room.Id, room.Code, room.Name, room.Floor,
            room.Status.ToVietnamese(), room.Status.ToActiveStatus(),
            room.Description, room.CreatedAt, room.UpdatedAt);
    }
}
