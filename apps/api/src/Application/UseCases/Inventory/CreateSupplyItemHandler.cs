using DentalClinic.API.Application.DTOs.Inventory;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Application.UseCases.Inventory;

public class CreateSupplyItemHandler(AppDbContext db)
{
    public async Task<SupplyItemDto> HandleAsync(CreateSupplyItemRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ValidationException("Tên vật tư không được để trống.");

        if (string.IsNullOrWhiteSpace(request.Code))
            throw new ValidationException("Mã vật tư không được để trống.");

        if (await db.SupplyItems.AnyAsync(s => s.Code == request.Code.Trim().ToUpper(), ct))
            throw new ConflictException($"Mã vật tư '{request.Code}' đã tồn tại.");

        var item = SupplyItem.Create(
            request.Code.Trim().ToUpper(),
            request.Name.Trim(),
            request.Category.Trim(),
            request.Unit.Trim(),
            request.Quantity,
            request.MinQuantity);

        db.SupplyItems.Add(item);
        await db.SaveChangesAsync(ct);

        return GetSupplyItemsHandler.ToDto(item);
    }
}
