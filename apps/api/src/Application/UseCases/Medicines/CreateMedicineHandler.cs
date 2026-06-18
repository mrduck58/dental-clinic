using DentalClinic.API.Application.DTOs.Medicines;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;

namespace DentalClinic.API.Application.UseCases.Medicines;

public class CreateMedicineHandler(IMedicineRepository repository)
{
    public async Task<MedicineDto> HandleAsync(CreateMedicineRequest request, CancellationToken ct = default)
    {
        var medicine = Medicine.Create(
            request.Name,
            request.GenericName,
            request.Manufacturer,
            request.Unit,
            request.Description);

        await repository.AddAsync(medicine, ct);
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
