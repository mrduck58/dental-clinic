namespace DentalClinic.API.Application.DTOs.Promotions;

public record PromotionDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    string DiscountType,
    decimal DiscountValue,
    List<Guid> ServiceIds,
    List<string> ServiceNames,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt
);

public record CreatePromotionRequest(
    string Code,
    string Name,
    string? Description,
    string DiscountType,
    decimal DiscountValue,
    List<Guid> ServiceIds,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsActive
);

public record UpdatePromotionRequest(
    string Code,
    string Name,
    string? Description,
    string DiscountType,
    decimal DiscountValue,
    List<Guid> ServiceIds,
    DateOnly StartDate,
    DateOnly EndDate
);
