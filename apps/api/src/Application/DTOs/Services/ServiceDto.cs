namespace DentalClinic.API.Application.DTOs.Services;

public record ServiceOptionDto(
    Guid Id,
    string Name,
    decimal Price,
    string Unit,
    int SortOrder);

public record ServiceDto(
    Guid Id,
    string Name,
    decimal Price,
    int DurationMinutes,
    bool IsActive,
    string Description,
    string Content,
    int ViewCount,
    string? ImageUrl,
    string? IconUrl,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    IReadOnlyCollection<ServiceOptionDto> Options,
    int? EstimatedSessionCount = null,
    int? EstimatedDurationMin = null,
    int? EstimatedDurationMax = null,
    string? EstimatedDurationUnit = null);

public record ServiceOptionRequest(
    string Name,
    decimal Price,
    string? Unit,
    int SortOrder);

public record CreateServiceRequest(
    string Name,
    decimal Price,
    int DurationMinutes,
    string Description,
    string Content,
    string? ImageUrl,
    string? IconUrl,
    IReadOnlyCollection<ServiceOptionRequest>? Options,
    int? EstimatedSessionCount = null,
    int? EstimatedDurationMin = null,
    int? EstimatedDurationMax = null,
    string? EstimatedDurationUnit = null);

public record UpdateServiceRequest(
    string Name,
    decimal Price,
    int DurationMinutes,
    string Description,
    string Content,
    string? ImageUrl,
    string? IconUrl,
    IReadOnlyCollection<ServiceOptionRequest>? Options,
    int? EstimatedSessionCount = null,
    int? EstimatedDurationMin = null,
    int? EstimatedDurationMax = null,
    string? EstimatedDurationUnit = null);
