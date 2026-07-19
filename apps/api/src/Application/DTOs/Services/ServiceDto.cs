namespace DentalClinic.API.Application.DTOs.Services;

public record ServiceDto(
    Guid Id,
    string Name,
    decimal Price,
    int DurationMinutes,
    bool IsActive,
    string Description,
    int ViewCount,
    string? ImageUrl,
    string? IconUrl,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public record CreateServiceRequest(
    string Name,
    decimal Price,
    int DurationMinutes,
    string Description,
    string? ImageUrl,
    string? IconUrl);

public record UpdateServiceRequest(
    string Name,
    decimal Price,
    int DurationMinutes,
    string Description,
    string? ImageUrl,
    string? IconUrl);
