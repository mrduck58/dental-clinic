using DentalClinic.API.Application.DTOs.Services;
using DentalClinic.API.Domain.Entities;

namespace DentalClinic.API.Application.UseCases.Services;

/// <summary>Maps a Service domain entity to the ServiceDto response object.</summary>
internal static class ServiceMapper
{
    public static ServiceDto ToDto(Service service)
        => new(
            service.Id,
            service.Name,
            service.Price,
            service.DurationMinutes,
            service.IsActive,
            service.Description,
            service.Content,
            service.ViewCount,
            service.ImageUrl,
            service.IconUrl,
            service.CreatedAt,
            service.UpdatedAt,
            service.Options
                .OrderBy(o => o.SortOrder)
                .Select(o => new ServiceOptionDto(o.Id, o.Name, o.Price, o.Unit, o.SortOrder))
                .ToList()
                .AsReadOnly(),
            service.EstimatedSessionCount,
            service.EstimatedDurationMin,
            service.EstimatedDurationMax,
            service.EstimatedDurationUnit?.ToString());
}
