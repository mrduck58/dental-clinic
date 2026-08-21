using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Inventory;

public record DeleteSupplyItemCommand(Guid Id) : IRequest;

/// <summary>
/// Xóa hẳn một vật tư khỏi danh mục — chỉ áp dụng được cho vật tư CHƯA từng phát sinh giao dịch/định mức
/// nào (khóa ngoại Restrict ở DB sẽ chặn nếu đã có SupplyTransaction/ServiceSupplyItem/TreatmentSupplyUsage
/// tham chiếu tới — <see cref="ISupplyItemRepository.DeleteAsync"/> dịch lỗi DB đó thành
/// <see cref="ValidationException"/> dễ hiểu thay vì để lộ lỗi 500).
/// </summary>
public class DeleteSupplyItemHandler(ISupplyItemRepository supplyItemRepository)
    : IRequestHandler<DeleteSupplyItemCommand>
{
    public async Task Handle(DeleteSupplyItemCommand command, CancellationToken ct)
    {
        var item = await supplyItemRepository.GetByIdAsync(command.Id, ct)
            ?? throw new NotFoundException("Không tìm thấy vật tư.");

        await supplyItemRepository.DeleteAsync(item, ct);
    }
}
