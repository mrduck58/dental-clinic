using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Domain.Constants;

namespace DentalClinic.API.Application.UseCases.Rooms;

public class DeleteRoomHandler(
    IRoomRepository roomRepository,
    IActivityLogService activityLogService,
    ICurrentUserService currentUser)
{
    public async Task HandleAsync(Guid id, CancellationToken ct = default)
    {
        var room = await roomRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Không tìm thấy phòng với ID: {id}");

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
            targetId: id.ToString(),
            ct: ct);
    }
}
