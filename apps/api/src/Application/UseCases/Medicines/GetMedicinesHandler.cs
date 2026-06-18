using DentalClinic.API.Application.DTOs.Medicines;
using DentalClinic.API.Domain.Interfaces.Repositories;

namespace DentalClinic.API.Application.UseCases.Medicines;

public class GetMedicinesHandler(IMedicineRepository medicineRepository)
{
    public async Task<IEnumerable<MedicineDto>> HandleAsync(
        string? search,
        CancellationToken ct = default)
    {
        var medicines = await medicineRepository.GetAllAsync(ct);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.ToLower();
            medicines = medicines.Where(m =>
                m.Name.ToLower().Contains(q) ||
                m.GenericName.ToLower().Contains(q) ||
                m.Manufacturer.ToLower().Contains(q));
        }

        return medicines.Select(m => new MedicineDto(
            m.Id, m.Name, m.GenericName, m.Manufacturer,
            m.Unit, m.Description, m.CreatedAt, m.UpdatedAt));
    }
}
