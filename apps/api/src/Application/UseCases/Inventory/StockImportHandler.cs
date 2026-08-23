using DentalClinic.API.Application.DTOs.Inventory;
using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Inventory;

public record StockImportCommand(
    string Name,
    string Unit,
    string Category,
    int Quantity,
    string? Note,
    decimal? UnitPrice,
    string CreatedBy) : IRequest<SupplyTransactionDto>;

public class StockImportHandler(
    ISupplyItemRepository supplyItemRepository,
    ISupplyTransactionRepository supplyTransactionRepository,
    IActivityLogService activityLogService,
    ICurrentUserService currentUser)
    : IRequestHandler<StockImportCommand, SupplyTransactionDto>
{
    public async Task<SupplyTransactionDto> Handle(StockImportCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
            throw new ValidationException("Tên vật tư không được để trống.");

        if (string.IsNullOrWhiteSpace(command.Unit) || !InventoryConstants.AllowedUnits.Contains(command.Unit))
            throw new ValidationException("Đơn vị không hợp lệ. Vui lòng chọn từ danh sách.");

        if (!InventoryConstants.AllowedCategories.Contains(command.Category))
            throw new ValidationException("Danh mục không hợp lệ. Vui lòng chọn từ danh sách.");

        if (command.Quantity <= 0)
            throw new ValidationException("Số lượng phải lớn hơn 0.");

        if (command.UnitPrice is null)
            throw new ValidationException("Vui lòng nhập đơn giá.");

        if (command.UnitPrice < 0)
            throw new ValidationException("Đơn giá không được âm.");

        var nameNorm = command.Name.Trim();

        var item = await supplyItemRepository.GetByNameAsync(nameNorm, ct);

        SupplyItem? newItem = null;
        if (item != null)
        {
            // Vật tư đã tồn tại — giữ nguyên đơn vị + loại vật tư đã phân, chỉ cộng số lượng.
            item.AdjustQuantity(command.Quantity);
            // Giá tham chiếu chỉ cập nhật theo lần nhập gần nhất với hàng "standard" (mua lại theo thời gian,
            // giá nhà cung cấp trôi nổi). Hàng "custom" (đặt riêng cho bệnh nhân) mỗi lần nhập thường là 1 ca
            // khác nhau với giá khác nhau — ghi đè Price ở đây sẽ đánh mất giá trị tham chiếu ban đầu một cách
            // sai lệch; giá thật của từng lần nhập vẫn tra được đúng qua SupplyTransaction.UnitPrice (tab
            // "Lịch sử giao dịch"), không mất dữ liệu.
            if (command.UnitPrice is decimal p && item.OrderType != "custom") item.UpdatePrice(p);
        }
        else
        {
            // Vật tư chưa tồn tại — tạo mới với đơn vị, danh mục, và giá được chọn. OrderType suy ra từ Danh mục.
            var code = "VT" + Guid.NewGuid().ToString("N")[..6].ToUpper();
            item = SupplyItem.Create(code, nameNorm, command.Category, command.Unit, command.Quantity, 5, command.UnitPrice);
            newItem = item;
        }

        var tx = SupplyTransaction.Create(item.Id, "import", command.Quantity, command.Note, command.CreatedBy, command.UnitPrice);

        // Một lần SaveChanges duy nhất (bên trong AddImportAsync) — EF Core tự bao trong implicit transaction.
        // item đã tồn tại thì đang được tracked (fetch không AsNoTracking) bởi cùng AppDbContext (scoped),
        // nên thay đổi AdjustQuantity()/UpdatePrice() ở trên cũng được lưu chung, atomic với việc tạo tx.
        await supplyTransactionRepository.AddImportAsync(newItem, tx, ct);

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
