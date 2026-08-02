using DentalClinic.API.Application.DTOs.Services;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Services;

public record GetServiceByIdQuery(Guid Id) : IRequest<ServiceDto>;

public class GetServiceByIdHandler(IServiceRepository serviceRepository) : IRequestHandler<GetServiceByIdQuery, ServiceDto>
{
    public async Task<ServiceDto> Handle(GetServiceByIdQuery request, CancellationToken cancellationToken)
    {
        var service = await serviceRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Không tìm thấy dịch vụ với ID: {request.Id}");

        return new ServiceDto(
            service.Id, service.Name, service.Price,
            service.DurationMinutes, service.IsActive, service.Description,
            service.ViewCount, service.ImageUrl, service.IconUrl, service.CreatedAt, service.UpdatedAt);
    }
}
