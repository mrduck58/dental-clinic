using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Services;

public record ServiceSupplyItemStepRequest(Guid SupplyItemId, int DefaultQuantity, string? ServiceOptionName = null);

public class ServiceSupplyItemDto
{
    public Guid Id { get; set; }
    public Guid ServiceId { get; set; }
    /// <summary>Option riêng mà dòng này áp dụng (vd: "Titan") — null = dùng chung cho mọi option.</summary>
    public string? ServiceOptionName { get; set; }
    public Guid SupplyItemId { get; set; }
    public string SupplyItemName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public int DefaultQuantity { get; set; }
}

/// <summary>Toàn bộ định mức của dịch vụ (mọi option) — dùng cho màn quản lý (Admin).</summary>
public record GetServiceSupplyItemsQuery(Guid ServiceId) : IRequest<List<ServiceSupplyItemDto>>;

/// <summary>
/// Định mức HIỆU LỰC khi đã biết option cụ thể (hoặc không chọn option nào) — dùng lúc bác sĩ xem
/// gợi ý/ghi nhận tiêu hao. Gồm các dòng dùng chung + các dòng khai riêng cho đúng option đó.
/// </summary>
public record GetEffectiveServiceSupplyItemsQuery(Guid ServiceId, string? OptionName) : IRequest<List<ServiceSupplyItemDto>>;

/// <summary>Thay toàn bộ định mức vật tư của một dịch vụ (xóa hết dòng cũ, thêm dòng mới).</summary>
public record ReplaceServiceSupplyItemsCommand(Guid ServiceId, List<ServiceSupplyItemStepRequest> Items) : IRequest<List<ServiceSupplyItemDto>>;

public class ServiceSupplyItemHandler(
    IServiceSupplyItemRepository serviceSupplyItemRepository,
    IServiceRepository serviceRepository,
    ISupplyItemRepository supplyItemRepository) :
    IRequestHandler<GetServiceSupplyItemsQuery, List<ServiceSupplyItemDto>>,
    IRequestHandler<GetEffectiveServiceSupplyItemsQuery, List<ServiceSupplyItemDto>>,
    IRequestHandler<ReplaceServiceSupplyItemsCommand, List<ServiceSupplyItemDto>>
{
    public async Task<List<ServiceSupplyItemDto>> Handle(GetServiceSupplyItemsQuery request, CancellationToken cancellationToken)
    {
        var items = await serviceSupplyItemRepository.GetByServiceIdAsync(request.ServiceId, cancellationToken);
        return ToDtos(items);
    }

    public async Task<List<ServiceSupplyItemDto>> Handle(GetEffectiveServiceSupplyItemsQuery request, CancellationToken cancellationToken)
    {
        var optionName = string.IsNullOrWhiteSpace(request.OptionName) ? null : request.OptionName.Trim();
        var items = await serviceSupplyItemRepository.GetEffectiveByServiceIdAsync(request.ServiceId, optionName, cancellationToken);
        return ToDtos(items);
    }

    public async Task<List<ServiceSupplyItemDto>> Handle(ReplaceServiceSupplyItemsCommand request, CancellationToken cancellationToken)
    {
        var serviceId = request.ServiceId;
        var items = request.Items;

        var service = await serviceRepository.GetByIdAsync(serviceId, cancellationToken);
        if (service is null)
            throw new NotFoundException("Không tìm thấy dịch vụ.");

        if (items.Any(i => i.DefaultQuantity <= 0))
            throw new ValidationException("Số lượng định mức phải lớn hơn 0.");

        var normalized = items
            .Select(i => (i.SupplyItemId, OptionName: string.IsNullOrWhiteSpace(i.ServiceOptionName) ? null : i.ServiceOptionName!.Trim(), i.DefaultQuantity))
            .ToList();

        if (normalized.Select(i => (i.SupplyItemId, i.OptionName)).Distinct().Count() != normalized.Count)
            throw new ValidationException("Không được khai trùng một vật tư nhiều lần cho cùng một option trong định mức.");

        var validOptionNames = service.Options.Select(o => o.Name).ToHashSet();
        if (normalized.Any(i => i.OptionName != null && !validOptionNames.Contains(i.OptionName)))
            throw new ValidationException("Option đã chọn không tồn tại trong danh sách option hiện tại của dịch vụ.");

        foreach (var i in normalized)
        {
            var supplyItem = await supplyItemRepository.GetByIdAsync(i.SupplyItemId, cancellationToken);
            if (supplyItem is null)
                throw new NotFoundException("Không tìm thấy vật tư trong danh mục.");
        }

        var newItems = normalized.Select(i => ServiceSupplyItem.Create(serviceId, i.SupplyItemId, i.DefaultQuantity, i.OptionName));

        await serviceSupplyItemRepository.ReplaceAllForServiceAsync(serviceId, newItems, cancellationToken);

        var saved = await serviceSupplyItemRepository.GetByServiceIdAsync(serviceId, cancellationToken);
        return ToDtos(saved);
    }

    private static List<ServiceSupplyItemDto> ToDtos(IEnumerable<ServiceSupplyItem> items) => items
        .Select(i => new ServiceSupplyItemDto
        {
            Id = i.Id,
            ServiceId = i.ServiceId,
            ServiceOptionName = i.ServiceOptionName,
            SupplyItemId = i.SupplyItemId,
            SupplyItemName = i.SupplyItem.Name,
            Unit = i.SupplyItem.Unit,
            DefaultQuantity = i.DefaultQuantity,
        })
        .ToList();
}
