using DentalClinic.API.Application.DTOs.Medicines;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Exceptions;

namespace DentalClinic.API.Application.UseCases.Medicines;

public class GetMedicineByIdHandler(IMedicineRepository medicineRepository)
{
    public async Task<MedicineDto> HandleAsync(Guid id, CancellationToken ct = default)
    {
        var medicine = await medicineRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Không tìm thấy thuốc.");

        return new MedicineDto(
            medicine.Id, medicine.Name, medicine.GenericName, medicine.Manufacturer,
            medicine.Unit, medicine.Description, medicine.CreatedAt, medicine.UpdatedAt);
    }
}
