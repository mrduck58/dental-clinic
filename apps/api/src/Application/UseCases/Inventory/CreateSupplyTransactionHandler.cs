using DentalClinic.API.Application.DTOs.Inventory;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Application.UseCases.Inventory;

public record CreateSupplyTransactionCommand(
    Guid SupplyItemId,
    string Type,
    int Quantity,
    string? Note,
    string CreatedBy) : IRequest<SupplyTransactionDto>;

public class CreateSupplyTransactionHandler(AppDbContext db, IActivityLogService activityLogService, ICurrentUserService currentUser)
    : IRequestHandler<CreateSupplyTransactionCommand, SupplyTransactionDto>
{
    public async Task<SupplyTransactionDto> Handle(CreateSupplyTransactionCommand command, CancellationToken ct)
    {
        if (command.Quantity <= 0)
            throw new ValidationException("Số lượng phải lớn hơn 0.");

        if (command.Type != "import" && command.Type != "export")
            throw new ValidationException("Loại giao dịch không hợp lệ.");

        var item = await db.SupplyItems.FirstOrDefaultAsync(s => s.Id == command.SupplyItemId, ct)
            ?? throw new NotFoundException("Không tìm thấy vật tư.");

        if (command.Type == "export" && command.Quantity > item.Quantity)
            throw new ValidationException($"Số lượng xuất ({command.Quantity}) vượt quá tồn kho hiện tại ({item.Quantity}).");

        var delta = command.Type == "import" ? command.Quantity : -command.Quantity;
        item.AdjustQuantity(delta);

        var tx = SupplyTransaction.Create(item.Id, command.Type, command.Quantity, command.Note, command.CreatedBy);
        db.SupplyTransactions.Add(tx);

        // Một lần SaveChanges duy nhất — atomic
        await db.SaveChangesAsync(ct);

        var actionType = command.Type == "import" ? "nhập kho" : "xuất kho";
        await activityLogService.LogAsync(
            userId: currentUser.UserId,
            userName: currentUser.UserName,
            userRole: currentUser.UserRole,
            action: ActivityAction.Create,
            module: ActivityModule.Inventory,
            description: $"{actionType}: {item.Name} x{command.Quantity} {(command.Note != null ? $"({command.Note})" : "")}",
            status: ActivityStatus.Success,
            ipAddress: currentUser.IpAddress,
            targetId: tx.Id.ToString(),
            ct: ct);

        return new SupplyTransactionDto(tx.Id, item.Id, item.Name, tx.Type, tx.Quantity, tx.UnitPrice, tx.Note, tx.CreatedBy, tx.CreatedAt);
    }
}
