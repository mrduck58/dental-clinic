using DentalClinic.API.Application.DTOs.Medicines;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Exceptions;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Medicines;

public record GetMedicineByIdQuery(Guid Id) : IRequest<MedicineDto>;

public class GetMedicineByIdHandler(IMedicineRepository medicineRepository)
    : IRequestHandler<GetMedicineByIdQuery, MedicineDto>
{
    public async Task<MedicineDto> Handle(GetMedicineByIdQuery request, CancellationToken ct)
    {
        var medicine = await medicineRepository.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException("Không tìm thấy thuốc.");

        return new MedicineDto(
            medicine.Id, medicine.Name, medicine.GenericName, medicine.Manufacturer,
            medicine.Unit, medicine.Description, medicine.CreatedAt, medicine.UpdatedAt);
    }
}
