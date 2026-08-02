using DentalClinic.API.Application.DTOs.Inventory;
using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Application.UseCases.Inventory;

public record StockImportCommand(
    string Name,
    string Unit,
    string Category,
    int Quantity,
    string? Note,
    decimal? UnitPrice,
    string? OrderType,
    string CreatedBy) : IRequest<SupplyTransactionDto>;

public class StockImportHandler(AppDbContext db, IActivityLogService activityLogService, ICurrentUserService currentUser)
    : IRequestHandler<StockImportCommand, SupplyTransactionDto>
{
    private static readonly string[] AllowedUnits = ["Cái", "Hộp", "Tuýp", "Cuộn", "Chai", "Gói", "Bộ"];
    private static readonly string[] AllowedOrderTypes = ["standard", "custom"];

    public async Task<SupplyTransactionDto> Handle(StockImportCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
            throw new ValidationException("Tên vật tư không được để trống.");

        if (string.IsNullOrWhiteSpace(command.Unit) || !AllowedUnits.Contains(command.Unit))
            throw new ValidationException("Đơn vị không hợp lệ. Vui lòng chọn từ danh sách.");

        if (string.IsNullOrWhiteSpace(command.Category))
            throw new ValidationException("Danh mục không được để trống.");

        if (command.Quantity <= 0)
            throw new ValidationException("Số lượng phải lớn hơn 0.");

        if (command.UnitPrice is < 0)
            throw new ValidationException("Đơn giá không được âm.");

        var orderType = string.IsNullOrWhiteSpace(command.OrderType) ? "standard" : command.OrderType.Trim();
        if (!AllowedOrderTypes.Contains(orderType))
            throw new ValidationException("Loại vật tư không hợp lệ. Chỉ chấp nhận: standard, custom.");

        var nameNorm = command.Name.Trim();

        var item = await db.SupplyItems
            .FirstOrDefaultAsync(s => s.Name.ToLower() == nameNorm.ToLower(), ct);

        if (item != null)
        {
            // Vật tư đã tồn tại — giữ nguyên đơn vị + loại vật tư đã phân, chỉ cộng số lượng.
            item.AdjustQuantity(command.Quantity);
            if (command.UnitPrice is decimal p) item.UpdatePrice(p); // giá tham chiếu cập nhật theo lần nhập gần nhất
        }
        else
        {
            // Vật tư chưa tồn tại — tạo mới với đơn vị, loại vật tư, và giá được chọn.
            var code = "VT" + Guid.NewGuid().ToString("N")[..6].ToUpper();
            item = SupplyItem.Create(code, nameNorm, command.Category.Trim(), command.Unit, command.Quantity, 5, orderType, command.UnitPrice);
            db.SupplyItems.Add(item);
        }

        var tx = SupplyTransaction.Create(item.Id, "import", command.Quantity, command.Note, command.CreatedBy, command.UnitPrice);
        db.SupplyTransactions.Add(tx);

        // Một lần SaveChanges duy nhất — EF Core tự bao trong implicit transaction
        await db.SaveChangesAsync(ct);

        await activityLogService.LogAsync(
            userId: currentUser.UserId,
            userName: currentUser.UserName,
            userRole: currentUser.UserRole,
            action: ActivityAction.Create,
            module: ActivityModule.Inventory,
            description: $"nhập kho: {item.Name} x{command.Quantity}{(command.Note != null ? $" ({command.Note})" : "")}",
            status: ActivityStatus.Success,
            ipAddress: currentUser.IpAddress,
            targetId: tx.Id.ToString(),
            ct: ct);

        return new SupplyTransactionDto(tx.Id, item.Id, item.Name, tx.Type, tx.Quantity, tx.UnitPrice, tx.Note, tx.CreatedBy, tx.CreatedAt);
    }
}
