using DentalClinic.API.Application.DTOs.Medicines;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Medicines;

public record GetMedicinesQuery(string? Search = null) : IRequest<IEnumerable<MedicineDto>>;

public class GetMedicinesHandler(IMedicineRepository medicineRepository)
    : IRequestHandler<GetMedicinesQuery, IEnumerable<MedicineDto>>
{
    public async Task<IEnumerable<MedicineDto>> Handle(GetMedicinesQuery request, CancellationToken ct)
    {
        var medicines = await medicineRepository.GetAllAsync(ct);

        var search = request.Search;
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
