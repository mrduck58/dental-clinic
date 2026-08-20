using DentalClinic.API.Application.DTOs.Inventory;
using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Inventory;

public record UpdateSupplyItemCommand(
    Guid Id,
    string Name,
    string Category,
    string Unit,
    int MinQuantity,
    decimal? Price) : IRequest<SupplyItemDto>;

/// <summary>
/// Sửa thông tin mô tả của vật tư (tên, danh mục, đơn vị, tồn tối thiểu, giá) — KHÔNG sửa được số lượng
/// tồn kho hay Mã vật tư ở đây, vì tồn kho chỉ được thay đổi qua giao dịch nhập/xuất/tiêu hao (có truy vết),
/// còn Mã là định danh cố định từ lúc tạo.
/// </summary>
public class UpdateSupplyItemHandler(ISupplyItemRepository supplyItemRepository)
    : IRequestHandler<UpdateSupplyItemCommand, SupplyItemDto>
{
    public async Task<SupplyItemDto> Handle(UpdateSupplyItemCommand command, CancellationToken ct)
    {
        var item = await supplyItemRepository.GetByIdAsync(command.Id, ct)
            ?? throw new NotFoundException("Không tìm thấy vật tư.");

        if (string.IsNullOrWhiteSpace(command.Name))
            throw new ValidationException("Tên vật tư không được để trống.");

        if (!InventoryConstants.AllowedCategories.Contains(command.Category))
            throw new ValidationException("Danh mục không hợp lệ. Vui lòng chọn từ danh sách.");

        if (string.IsNullOrWhiteSpace(command.Unit) || !InventoryConstants.AllowedUnits.Contains(command.Unit))
            throw new ValidationException("Đơn vị không hợp lệ. Vui lòng chọn từ danh sách.");

        if (command.MinQuantity < 0)
            throw new ValidationException("Tồn tối thiểu không được âm.");

        if (command.Price is < 0)
            throw new ValidationException("Giá tiền không được âm.");

        // Update() tự suy ra lại OrderType từ Danh mục mới (xem SupplyItem.Update).
        item.Update(command.Name.Trim(), command.Category, command.Unit, command.MinQuantity);
        if (command.Price is decimal p) item.UpdatePrice(p);

        await supplyItemRepository.UpdateAsync(item, ct);

        return GetSupplyItemsHandler.ToDto(item);
    }
}
