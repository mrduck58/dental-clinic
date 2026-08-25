using DentalClinic.API.Application.DTOs.Inventory;
using DentalClinic.API.Domain.Common;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Inventory;

/// <summary>RoomId khác null → chỉ trả về giao dịch xuất theo đúng phòng đó (dùng cho màn chi tiết phòng
/// bên Admin, xem `admin/rooms/[id]`).</summary>
public record GetSupplyTransactionsQuery(Guid? RoomId = null) : IRequest<IEnumerable<SupplyTransactionDto>>;

/// <summary>Các lần nhập kho trong một khoảng ngày — drill-down của thẻ/cột "Vật tư" bên Chi phí. Dùng
/// đúng khoảng UTC mà ExpenseQueryService.TotalSupply đã tính (Domain.Common.VietnamPeriod) nên tổng các
/// dòng trả về ở đây luôn khớp con số hiển thị trên trang Chi phí, không lệch múi giờ.</summary>
public record GetSupplyImportsInRangeQuery(DateOnly From, DateOnly To) : IRequest<IEnumerable<SupplyTransactionDto>>;

public class GetSupplyTransactionsHandler(ISupplyTransactionRepository repository)
    : IRequestHandler<GetSupplyTransactionsQuery, IEnumerable<SupplyTransactionDto>>,
      IRequestHandler<GetSupplyImportsInRangeQuery, IEnumerable<SupplyTransactionDto>>
{
    public async Task<IEnumerable<SupplyTransactionDto>> Handle(GetSupplyTransactionsQuery request, CancellationToken ct)
    {
        var txs = await repository.GetAllAsync(request.RoomId, ct);
        return txs.Select(ToDto);
    }

    public async Task<IEnumerable<SupplyTransactionDto>> Handle(GetSupplyImportsInRangeQuery request, CancellationToken ct)
    {
        var (start, end) = VietnamPeriod.Bounds(request.From, request.To);
        var txs = await repository.GetImportsInRangeAsync(start, end, ct);
        return txs.Select(ToDto);
    }

    private static SupplyTransactionDto ToDto(Domain.Entities.SupplyTransaction t) => new(
        t.Id,
        t.SupplyItemId,
        t.SupplyItem.Name,
        t.Type,
        t.Quantity,
        t.UnitPrice,
        t.Note,
        t.CreatedBy,
        t.CreatedAt,
        t.Room?.Name);
}
