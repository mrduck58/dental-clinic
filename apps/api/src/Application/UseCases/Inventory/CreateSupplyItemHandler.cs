using DentalClinic.API.Application.DTOs.Inventory;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Application.UseCases.Inventory;

public class CreateSupplyItemHandler(AppDbContext db)
{
    private static readonly string[] AllowedOrderTypes = ["standard", "custom"];

    public async Task<SupplyItemDto> HandleAsync(CreateSupplyItemRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ValidationException("Tên vật tư không được để trống.");

        if (string.IsNullOrWhiteSpace(request.Code))
            throw new ValidationException("Mã vật tư không được để trống.");

        var orderType = string.IsNullOrWhiteSpace(request.OrderType) ? "standard" : request.OrderType.Trim();
        if (!AllowedOrderTypes.Contains(orderType))
            throw new ValidationException("Loại vật tư không hợp lệ. Chỉ chấp nhận: standard, custom.");

        if (request.Price is < 0)
            throw new ValidationException("Giá tiền không được âm.");

        if (await db.SupplyItems.AnyAsync(s => s.Code == request.Code.Trim().ToUpper(), ct))
            throw new ConflictException($"Mã vật tư '{request.Code}' đã tồn tại.");

        var item = SupplyItem.Create(
            request.Code.Trim().ToUpper(),
            request.Name.Trim(),
            request.Category.Trim(),
            request.Unit.Trim(),
            request.Quantity,
            request.MinQuantity,
            orderType,
            request.Price);

        db.SupplyItems.Add(item);
        await db.SaveChangesAsync(ct);

        return GetSupplyItemsHandler.ToDto(item);
    }
}
