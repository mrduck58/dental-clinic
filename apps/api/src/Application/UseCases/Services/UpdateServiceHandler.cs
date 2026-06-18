using DentalClinic.API.Application.DTOs.Services;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;

namespace DentalClinic.API.Application.UseCases.Services;

public class UpdateServiceHandler(IServiceRepository serviceRepository)
{
    public async Task<ServiceDto> HandleAsync(Guid id, UpdateServiceRequest request, CancellationToken ct = default)
    {
        var service = await serviceRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Không tìm thấy dịch vụ với ID: {id}");

        service.Update(
            request.Name,
            request.Price,
            request.DurationMinutes,
            request.Description,
            request.ImageUrl);

        await serviceRepository.UpdateAsync(service, ct);

        return new ServiceDto(
            service.Id, service.Name, service.Price,
            service.DurationMinutes, service.IsActive, service.Description,
            service.ViewCount, service.ImageUrl, service.CreatedAt, service.UpdatedAt);
    }
}
