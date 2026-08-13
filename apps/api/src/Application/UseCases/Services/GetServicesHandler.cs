using DentalClinic.API.Application.DTOs.Services;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Services;

public record GetServicesQuery(string? Status, string? Search) : IRequest<IEnumerable<ServiceDto>>;

public class GetServicesHandler(IServiceRepository serviceRepository) : IRequestHandler<GetServicesQuery, IEnumerable<ServiceDto>>
{
    public async Task<IEnumerable<ServiceDto>> Handle(GetServicesQuery request, CancellationToken cancellationToken)
    {
        var services = await serviceRepository.GetAllAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var isActive = request.Status.Equals("Active", StringComparison.OrdinalIgnoreCase);
            services = services.Where(s => s.IsActive == isActive);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var q = request.Search.ToLower();
            services = services.Where(s =>
                s.Name.ToLower().Contains(q) ||
                s.Description.ToLower().Contains(q));
        }

        return services.Select(s => ServiceMapper.ToDto(s));
    }
}
