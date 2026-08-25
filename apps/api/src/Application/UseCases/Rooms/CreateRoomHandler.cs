using DentalClinic.API.Application.DTOs.Rooms;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Domain.Constants;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Rooms;

public record CreateRoomCommand(CreateRoomRequest Request) : IRequest<RoomDto>;

public class CreateRoomHandler(IRoomRepository roomRepository, IActivityLogService activityLogService, ICurrentUserService currentUser) : IRequestHandler<CreateRoomCommand, RoomDto>
{
    public async Task<RoomDto> Handle(CreateRoomCommand command, CancellationToken ct)
    {
        var request = command.Request;

        if (await roomRepository.ExistsByCodeAsync(request.Code, ct: ct))
            throw new ConflictException($"Mã phòng '{request.Code.ToUpperInvariant()}' đã tồn tại.");

        if (await roomRepository.ExistsByNameAsync(request.Name, ct: ct))
            throw new ConflictException($"Tên phòng '{request.Name}' đã tồn tại.");

        var room = Room.Create(
            request.Code,
            request.Name,
            request.Floor,
            request.Description);

        await roomRepository.AddAsync(room, ct);

        await activityLogService.LogAsync(
            userId: currentUser.UserId,
            userName: currentUser.UserName,
            userRole: currentUser.UserRole,
            action: ActivityAction.Create,
            module: ActivityModule.Room,
            description: $"Tạo phòng mới: {request.Name} (mã: {request.Code})",
            status: ActivityStatus.Success,
            ipAddress: currentUser.IpAddress,
            targetId: room.Id.ToString(),
            ct: ct);

        return new RoomDto(
            room.Id, room.Code, room.Name, room.Floor,
            room.Status.ToVietnamese(), room.Status.ToActiveStatus(),
            room.Description, room.CreatedAt, room.UpdatedAt);
    }
}
