using DentalClinic.API.Application.DTOs.Inventory;
using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Application.UseCases.Inventory;

public class StockImportHandler(AppDbContext db, IActivityLogService activityLogService, ICurrentUserService currentUser)
{
    private static readonly string[] AllowedUnits = ["Cái", "Hộp", "Tuýp", "Cuộn", "Chai", "Gói", "Bộ"];

    public async Task<SupplyTransactionDto> HandleAsync(
        StockImportRequest request,
        string createdBy,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ValidationException("Tên vật tư không được để trống.");

        if (string.IsNullOrWhiteSpace(request.Unit) || !AllowedUnits.Contains(request.Unit))
            throw new ValidationException("Đơn vị không hợp lệ. Vui lòng chọn từ danh sách.");

        if (string.IsNullOrWhiteSpace(request.Category))
            throw new ValidationException("Danh mục không được để trống.");

        if (request.Quantity <= 0)
            throw new ValidationException("Số lượng phải lớn hơn 0.");

        var nameNorm = request.Name.Trim();

        var item = await db.SupplyItems
            .FirstOrDefaultAsync(s => s.Name.ToLower() == nameNorm.ToLower(), ct);

        if (item != null)
        {
            // Vật tư đã tồn tại — giữ nguyên đơn vị, cộng số lượng
            item.AdjustQuantity(request.Quantity);
        }
        else
        {
            // Vật tư chưa tồn tại — tạo mới với đơn vị được chọn
            var code = "VT" + Guid.NewGuid().ToString("N")[..6].ToUpper();
            item = SupplyItem.Create(code, nameNorm, request.Category.Trim(), request.Unit, request.Quantity, 5);
            db.SupplyItems.Add(item);
        }

        var tx = SupplyTransaction.Create(item.Id, "import", request.Quantity, request.Note, createdBy);
        db.SupplyTransactions.Add(tx);

        // Một lần SaveChanges duy nhất — EF Core tự bao trong implicit transaction
        await db.SaveChangesAsync(ct);

        await activityLogService.LogAsync(
            userId: currentUser.UserId,
            userName: currentUser.UserName,
            userRole: currentUser.UserRole,
            action: ActivityAction.Create,
            module: ActivityModule.Inventory,
            description: $"nhập kho: {item.Name} x{request.Quantity}{(request.Note != null ? $" ({request.Note})" : "")}",
            status: ActivityStatus.Success,
            ipAddress: currentUser.IpAddress,
            targetId: tx.Id.ToString(),
            ct: ct);

        return new SupplyTransactionDto(tx.Id, item.Id, item.Name, tx.Type, tx.Quantity, tx.Note, tx.CreatedBy, tx.CreatedAt);
    }
}
