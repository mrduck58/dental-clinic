using DentalClinic.API.Application.DTOs.Services;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Domain.Constants;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Services;

public record UpdateServiceCommand(
    Guid Id,
    string Name,
    decimal Price,
    int DurationMinutes,
    string Description,
    string Content,
    string? ImageUrl,
    string? IconUrl,
    IReadOnlyCollection<ServiceOptionRequest>? Options) : IRequest<ServiceDto>;

public class UpdateServiceHandler(
    IServiceRepository serviceRepository,
    IActivityLogService activityLogService,
    ICurrentUserService currentUser) : IRequestHandler<UpdateServiceCommand, ServiceDto>
{
    public async Task<ServiceDto> Handle(UpdateServiceCommand request, CancellationToken cancellationToken)
    {
        var service = await serviceRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Không tìm thấy dịch vụ với ID: {request.Id}");

        service.Update(
            request.Name,
            request.Price,
            request.DurationMinutes,
            request.Description,
            request.Content ?? string.Empty,
            request.ImageUrl,
            request.IconUrl);

        // Thay toàn bộ tuỳ chọn bằng ĐÚNG MỘT thao tác trên aggregate: ReplaceOptions xoá sạch
        // collection rồi thêm bản mới, EF tự nhận ra cái nào là mồ côi (quan hệ Cascade) để xoá và
        // cái nào là mới để thêm, tất cả trong cùng một SaveChanges.
        //
        // Trước đây có thêm một lượt DeleteOptionsAsync + SaveChanges riêng chạy TRƯỚC. Nó xoá hàng
        // dưới DB nhưng instance cũ vẫn nằm trong service.Options; đến khi ReplaceOptions gọi Clear(),
        // EF lại coi chúng là mồ côi và phát tiếp câu DELETE cho những hàng vừa biến mất → 0 dòng bị
        // ảnh hưởng → DbUpdateConcurrencyException → người dùng nhận 500 mỗi lần cập nhật dịch vụ.
        //
        // Danh sách rỗng cũng phải chạy qua đây, nếu không thì xoá hết tuỳ chọn sẽ không lưu được gì.
        var newOptions = (request.Options ?? [])
            .OrderBy(o => o.SortOrder)
            .Select((o, i) => (o.Name, o.Price, Unit: o.Unit ?? "Răng", SortOrder: i));
        service.ReplaceOptions(newOptions);

        await serviceRepository.UpdateAsync(service, cancellationToken);

        await activityLogService.LogAsync(
            userId: currentUser.UserId,
            userName: currentUser.UserName,
            userRole: currentUser.UserRole,
            action: ActivityAction.Edit,
            module: ActivityModule.Service,
            description: $"Cập nhật dịch vụ: {service.Name}",
            status: ActivityStatus.Success,
            ipAddress: currentUser.IpAddress,
            targetId: request.Id.ToString(),
            ct: cancellationToken);

        // Reload to get fresh options list
        var updated = await serviceRepository.GetByIdAsync(service.Id, cancellationToken) ?? service;
        return ServiceMapper.ToDto(updated);
    }
}
