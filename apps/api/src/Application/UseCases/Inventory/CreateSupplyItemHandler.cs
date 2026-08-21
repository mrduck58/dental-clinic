using DentalClinic.API.Application.DTOs.Inventory;
using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Inventory;

public record CreateSupplyItemCommand(
    string Code,
    string Name,
    string Category,
    string Unit,
    int Quantity,
    int MinQuantity,
    decimal? Price = null) : IRequest<SupplyItemDto>;

public class CreateSupplyItemHandler(ISupplyItemRepository supplyItemRepository) : IRequestHandler<CreateSupplyItemCommand, SupplyItemDto>
{
    public async Task<SupplyItemDto> Handle(CreateSupplyItemCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
            throw new ValidationException("Tên vật tư không được để trống.");

        if (string.IsNullOrWhiteSpace(command.Code))
            throw new ValidationException("Mã vật tư không được để trống.");

        if (!InventoryConstants.AllowedCategories.Contains(command.Category))
            throw new ValidationException("Danh mục không hợp lệ. Vui lòng chọn từ danh sách.");

        if (command.Price is < 0)
            throw new ValidationException("Giá tiền không được âm.");

        if (await supplyItemRepository.ExistsByCodeAsync(command.Code.Trim().ToUpper(), ct))
            throw new ConflictException($"Mã vật tư '{command.Code}' đã tồn tại.");

        // OrderType không cho chọn tay — SupplyItem.Create tự suy ra từ Danh mục.
        var item = SupplyItem.Create(
            command.Code.Trim().ToUpper(),
            command.Name.Trim(),
            command.Category,
            command.Unit.Trim(),
            command.Quantity,
            command.MinQuantity,
            command.Price);

        await supplyItemRepository.AddAsync(item, ct);

        return GetSupplyItemsHandler.ToDto(item);
    }
}
