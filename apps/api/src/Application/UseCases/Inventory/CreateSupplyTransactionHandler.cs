using DentalClinic.API.Application.DTOs.Inventory;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Domain.Constants;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Inventory;

public record CreateSupplyTransactionCommand(
    Guid SupplyItemId,
    string Type,
    int Quantity,
    string? Note,
    string CreatedBy,
    Guid? RoomId = null) : IRequest<SupplyTransactionDto>;

public class CreateSupplyTransactionHandler(
    ISupplyItemRepository supplyItemRepository,
    ISupplyTransactionRepository supplyTransactionRepository,
    IRoomRepository roomRepository,
    IActivityLogService activityLogService,
    ICurrentUserService currentUser)
    : IRequestHandler<CreateSupplyTransactionCommand, SupplyTransactionDto>
{
    public async Task<SupplyTransactionDto> Handle(CreateSupplyTransactionCommand command, CancellationToken ct)
    {
        if (command.Quantity <= 0)
            throw new ValidationException("Số lượng phải lớn hơn 0.");

        if (command.Type != "import" && command.Type != "export")
            throw new ValidationException("Loại giao dịch không hợp lệ.");

        if (command.RoomId is Guid roomId && command.Type != "export")
            throw new ValidationException("Chỉ xuất kho mới được gắn phòng nhận.");

        var item = await supplyItemRepository.GetByIdAsync(command.SupplyItemId, ct)
            ?? throw new NotFoundException("Không tìm thấy vật tư.");

        if (command.Type == "export" && command.Quantity > item.Quantity)
            throw new ValidationException($"Số lượng xuất ({command.Quantity}) vượt quá tồn kho hiện tại ({item.Quantity}).");

        Room? room = null;
        if (command.RoomId is Guid rid)
            room = await roomRepository.GetByIdAsync(rid, ct) ?? throw new NotFoundException("Không tìm thấy phòng.");

        var delta = command.Type == "import" ? command.Quantity : -command.Quantity;
        item.AdjustQuantity(delta);

        var tx = SupplyTransaction.Create(item.Id, command.Type, command.Quantity, command.Note, command.CreatedBy, roomId: room?.Id);

        // item đang được tracked (fetch không AsNoTracking) bởi cùng AppDbContext (scoped) — AddAsync bên dưới
        // gọi SaveChangesAsync 1 lần duy nhất sẽ lưu luôn cả thay đổi AdjustQuantity() ở trên → atomic.
        await supplyTransactionRepository.AddAsync(tx, ct);

        var actionType = command.Type == "import" ? "nhập kho" : "xuất kho";
        var roomSuffix = room != null ? $" cho phòng {room.Name}" : "";
        await activityLogService.LogAsync(
            userId: currentUser.UserId,
            userName: currentUser.UserName,
            userRole: currentUser.UserRole,
            action: ActivityAction.Create,
            module: ActivityModule.Inventory,
            description: $"{actionType}: {item.Name} x{command.Quantity}{roomSuffix} {(command.Note != null ? $"({command.Note})" : "")}",
            status: ActivityStatus.Success,
            ipAddress: currentUser.IpAddress,
            targetId: tx.Id.ToString(),
            ct: ct);

        return new SupplyTransactionDto(tx.Id, item.Id, item.Name, tx.Type, tx.Quantity, tx.UnitPrice, tx.Note, tx.CreatedBy, tx.CreatedAt, room?.Name);
    }
}
