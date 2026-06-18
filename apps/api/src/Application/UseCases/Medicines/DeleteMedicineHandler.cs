using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Exceptions;

namespace DentalClinic.API.Application.UseCases.Medicines;

public class DeleteMedicineHandler(IMedicineRepository medicineRepository)
{
    public async Task HandleAsync(Guid id, CancellationToken ct = default)
    {
        var medicine = await medicineRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Không tìm thấy thuốc.");

        await medicineRepository.DeleteAsync(medicine, ct);
    }
}
