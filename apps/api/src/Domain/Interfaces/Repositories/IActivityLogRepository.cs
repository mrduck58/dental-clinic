using DentalClinic.API.Domain.Entities;

namespace DentalClinic.API.Domain.Interfaces.Repositories;

public interface IActivityLogRepository
{
    Task AddAsync(ActivityLog log, CancellationToken ct = default);

    /// <param name="sortDir">"asc" | "desc" theo thời gian ghi nhận — mặc định "desc" (mới nhất trước).</param>
    /// <param name="excludeAction">Loại thao tác cần loại khỏi kết quả (vd "login" — Audit Log không hiển thị
    /// log đăng nhập nữa vì đã có màn hình Lịch sử đăng nhập riêng).</param>
    Task<(IReadOnlyList<ActivityLog> Items, int TotalCount)> GetPagedAsync(
        Guid? userId,
        string? action,
        string? module,
        string? status,
        string? search,
        DateTimeOffset? startDate,
        DateTimeOffset? endDate,
        int page,
        int pageSize,
        string? sortDir = null,
        string? excludeAction = null,
        CancellationToken ct = default);
}
