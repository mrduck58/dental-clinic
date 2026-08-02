using DentalClinic.API.Application.DTOs.Medicines;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Domain.Constants;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Medicines;

public record UpdateMedicineCommand(
    Guid Id,
    string Name,
    string GenericName,
    string Manufacturer,
    string Unit,
    string Description) : IRequest<MedicineDto>;

public class UpdateMedicineHandler(
    IMedicineRepository repository,
    IActivityLogService activityLogService,
    ICurrentUserService currentUser) : IRequestHandler<UpdateMedicineCommand, MedicineDto>
{
    public async Task<MedicineDto> Handle(UpdateMedicineCommand request, CancellationToken ct)
    {
        var medicine = await repository.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException("Không tìm thấy thuốc.");

        medicine.Update(
            request.Name,
            request.GenericName,
            request.Manufacturer,
            request.Unit,
            request.Description);

        await repository.UpdateAsync(medicine, ct);

        await activityLogService.LogAsync(
            userId: currentUser.UserId,
            userName: currentUser.UserName,
            userRole: currentUser.UserRole,
            action: ActivityAction.Edit,
            module: ActivityModule.Medicine,
            description: $"Cập nhật thuốc: {medicine.Name}",
            status: ActivityStatus.Success,
            ipAddress: currentUser.IpAddress,
            targetId: request.Id.ToString(),
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
