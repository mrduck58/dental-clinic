using DentalClinic.API.Application.DTOs.Medicines;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Domain.Constants;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Medicines;

public record CreateMedicineCommand(
    string Name,
    string GenericName,
    string Manufacturer,
    string Unit,
    string Description) : IRequest<MedicineDto>;

public class CreateMedicineHandler(IMedicineRepository repository, IActivityLogService activityLogService, ICurrentUserService currentUser)
    : IRequestHandler<CreateMedicineCommand, MedicineDto>
{
    public async Task<MedicineDto> Handle(CreateMedicineCommand request, CancellationToken ct)
    {
        var medicine = Medicine.Create(
            request.Name,
            request.GenericName,
            request.Manufacturer,
            request.Unit,
            request.Description);

        await repository.AddAsync(medicine, ct);

        await activityLogService.LogAsync(
            userId: currentUser.UserId,
            userName: currentUser.UserName,
            userRole: currentUser.UserRole,
            action: ActivityAction.Create,
            module: ActivityModule.Medicine,
            description: $"Thêm thuốc mới: {request.Name}",
            status: ActivityStatus.Success,
            ipAddress: currentUser.IpAddress,
            targetId: medicine.Id.ToString(),
            ct: ct);

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
