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
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public record CreateServiceRequest(
    string Name,
    decimal Price,
    int DurationMinutes,
    string Description,
    string? ImageUrl);

public record UpdateServiceRequest(
    string Name,
    decimal Price,
    int DurationMinutes,
    string Description,
    string? ImageUrl);
