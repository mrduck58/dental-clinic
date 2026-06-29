using DentalClinic.API.Application.DTOs.Inventory;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Application.UseCases.Inventory;

public class CreateSupplyTransactionHandler(AppDbContext db)
{
    public async Task<SupplyTransactionDto> HandleAsync(
        CreateSupplyTransactionRequest request,
        string createdBy,
        CancellationToken ct = default)
    {
        if (request.Quantity <= 0)
            throw new ValidationException("Số lượng phải lớn hơn 0.");

        if (request.Type != "import" && request.Type != "export")
            throw new ValidationException("Loại giao dịch không hợp lệ.");

        var item = await db.SupplyItems.FirstOrDefaultAsync(s => s.Id == request.SupplyItemId, ct)
            ?? throw new NotFoundException("Không tìm thấy vật tư.");

        if (request.Type == "export" && request.Quantity > item.Quantity)
            throw new ValidationException($"Số lượng xuất ({request.Quantity}) vượt quá tồn kho hiện tại ({item.Quantity}).");

        var delta = request.Type == "import" ? request.Quantity : -request.Quantity;
        item.AdjustQuantity(delta);

        var tx = SupplyTransaction.Create(item.Id, request.Type, request.Quantity, request.Note, createdBy);
        db.SupplyTransactions.Add(tx);

        // Một lần SaveChanges duy nhất — atomic
        await db.SaveChangesAsync(ct);

        return new SupplyTransactionDto(tx.Id, item.Id, item.Name, tx.Type, tx.Quantity, tx.Note, tx.CreatedBy, tx.CreatedAt);
    }
}
