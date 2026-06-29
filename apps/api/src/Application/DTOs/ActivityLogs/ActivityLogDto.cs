namespace DentalClinic.API.Application.DTOs.ActivityLogs;

public record ActivityLogDto(
    int Id,
    Guid? UserId,
    string UserName,
    string UserRole,
    string Action,
    string Module,
    string Description,
    string? IpAddress,
    string Status,
    string? TargetId,
    DateTimeOffset CreatedAt);

public record ActivityLogPagedDto(
    IReadOnlyList<ActivityLogDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);
