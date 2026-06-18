using DentalClinic.API.Application.DTOs.Medicines;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;

namespace DentalClinic.API.Application.UseCases.Medicines;

public class UpdateMedicineHandler(IMedicineRepository repository)
{
    public async Task<MedicineDto> HandleAsync(Guid id, UpdateMedicineRequest request, CancellationToken ct = default)
    {
        var medicine = await repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Không tìm thấy thuốc.");

        medicine.Update(
            request.Name,
            request.GenericName,
            request.Manufacturer,
            request.Unit,
            request.Description);

        await repository.UpdateAsync(medicine, ct);

        return new MedicineDto(
            medicine.Id,
            medicine.Name,
            medicine.GenericName,
            medicine.Manufacturer,
            medicine.Unit,
            medicine.Description,
            medicine.CreatedAt,
            medicine.UpdatedAt);
    }
}
