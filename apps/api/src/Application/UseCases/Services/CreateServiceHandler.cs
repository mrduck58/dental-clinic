using DentalClinic.API.Application.DTOs.Services;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;

namespace DentalClinic.API.Application.UseCases.Services;

public class CreateServiceHandler(IServiceRepository serviceRepository)
{
    public async Task<ServiceDto> HandleAsync(CreateServiceRequest request, CancellationToken ct = default)
    {
        var service = Service.Create(
            request.Name,
            request.Category,
            request.Price,
            request.DurationMinutes,
            request.Description,
            request.ImageUrl);

        await serviceRepository.AddAsync(service, ct);

        return new ServiceDto(
            service.Id, service.Name, service.Category, service.Price,
            service.DurationMinutes, service.IsActive, service.Description,
            service.ViewCount, service.ImageUrl, service.CreatedAt, service.UpdatedAt);
    }
}
