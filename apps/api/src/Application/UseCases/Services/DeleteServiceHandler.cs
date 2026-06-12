using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;

namespace DentalClinic.API.Application.UseCases.Services;

public class DeleteServiceHandler(IServiceRepository serviceRepository)
{
    public async Task HandleAsync(Guid id, CancellationToken ct = default)
    {
        var service = await serviceRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Không tìm thấy dịch vụ với ID: {id}");

        await serviceRepository.DeleteAsync(service, ct);
    }
}
