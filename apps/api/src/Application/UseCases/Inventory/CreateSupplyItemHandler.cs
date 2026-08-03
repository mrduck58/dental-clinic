using DentalClinic.API.Application.DTOs.Inventory;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Application.UseCases.Inventory;

public record CreateSupplyItemCommand(
    string Code,
    string Name,
    string Category,
    string Unit,
    int Quantity,
    int MinQuantity,
    string? OrderType = null,
    decimal? Price = null) : IRequest<SupplyItemDto>;

public class CreateSupplyItemHandler(AppDbContext db) : IRequestHandler<CreateSupplyItemCommand, SupplyItemDto>
{
    private static readonly string[] AllowedOrderTypes = ["standard", "custom"];

    public async Task<SupplyItemDto> Handle(CreateSupplyItemCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
            throw new ValidationException("Tên vật tư không được để trống.");

        if (string.IsNullOrWhiteSpace(command.Code))
            throw new ValidationException("Mã vật tư không được để trống.");

        var orderType = string.IsNullOrWhiteSpace(command.OrderType) ? "standard" : command.OrderType.Trim();
        if (!AllowedOrderTypes.Contains(orderType))
            throw new ValidationException("Loại vật tư không hợp lệ. Chỉ chấp nhận: standard, custom.");

        if (command.Price is < 0)
            throw new ValidationException("Giá tiền không được âm.");

        if (await db.SupplyItems.AnyAsync(s => s.Code == command.Code.Trim().ToUpper(), ct))
            throw new ConflictException($"Mã vật tư '{command.Code}' đã tồn tại.");

        var item = SupplyItem.Create(
            command.Code.Trim().ToUpper(),
            command.Name.Trim(),
            command.Category.Trim(),
            command.Unit.Trim(),
            command.Quantity,
            command.MinQuantity,
            orderType,
            command.Price);

        db.SupplyItems.Add(item);
        await db.SaveChangesAsync(ct);

        return GetSupplyItemsHandler.ToDto(item);
    }
}
