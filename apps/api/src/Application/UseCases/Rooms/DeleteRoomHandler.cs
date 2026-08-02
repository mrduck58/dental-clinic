using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Domain.Constants;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Rooms;

public record DeleteRoomCommand(Guid Id) : IRequest;

public class DeleteRoomHandler(
    IRoomRepository roomRepository,
    IActivityLogService activityLogService,
    ICurrentUserService currentUser) : IRequestHandler<DeleteRoomCommand>
{
    public async Task Handle(DeleteRoomCommand command, CancellationToken ct)
    {
        var room = await roomRepository.GetByIdAsync(command.Id, ct)
            ?? throw new NotFoundException($"Không tìm thấy phòng với ID: {command.Id}");

        await roomRepository.DeleteAsync(room, ct);

        await activityLogService.LogAsync(
            userId: currentUser.UserId,
            userName: currentUser.UserName,
            userRole: currentUser.UserRole,
            action: ActivityAction.Delete,
            module: ActivityModule.Room,
            description: $"Xóa phòng: {room.Name}",
            status: ActivityStatus.Success,
            ipAddress: currentUser.IpAddress,
            targetId: command.Id.ToString(),
            ct: ct);
    }
}
