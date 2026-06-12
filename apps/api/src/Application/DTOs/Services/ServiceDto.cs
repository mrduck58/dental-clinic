namespace DentalClinic.API.Application.DTOs.Services;

public record ServiceDto(
    Guid Id,
    string Name,
    string Category,
    decimal Price,
    int DurationMinutes,
    bool IsActive,
    string Description,
    int ViewCount,
    string? ImageUrl,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public record CreateServiceRequest(
    string Name,
    string Category,
    decimal Price,
    int DurationMinutes,
    string Description,
    string? ImageUrl);

public record UpdateServiceRequest(
    string Name,
    string Category,
    decimal Price,
    int DurationMinutes,
    string Description,
    string? ImageUrl);
