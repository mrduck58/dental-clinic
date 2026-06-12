using DentalClinic.API.Application.DTOs.Services;
using DentalClinic.API.Domain.Interfaces.Repositories;

namespace DentalClinic.API.Application.UseCases.Services;

public class GetServicesHandler(IServiceRepository serviceRepository)
{
    public async Task<IEnumerable<ServiceDto>> HandleAsync(
        string? category,
        string? status,
        string? search,
        CancellationToken ct = default)
    {
        var services = await serviceRepository.GetAllAsync(ct);

        if (!string.IsNullOrWhiteSpace(category))
            services = services.Where(s => s.Category == category);

        if (!string.IsNullOrWhiteSpace(status))
        {
            var isActive = status.Equals("Active", StringComparison.OrdinalIgnoreCase);
            services = services.Where(s => s.IsActive == isActive);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.ToLower();
            services = services.Where(s =>
                s.Name.ToLower().Contains(q) ||
                s.Description.ToLower().Contains(q));
        }

        return services.Select(s => new ServiceDto(
            s.Id, s.Name, s.Category, s.Price,
            s.DurationMinutes, s.IsActive, s.Description,
            s.ViewCount, s.ImageUrl, s.CreatedAt, s.UpdatedAt));
    }
}
