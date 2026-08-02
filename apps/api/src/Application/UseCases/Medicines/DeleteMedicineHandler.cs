using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Domain.Constants;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Medicines;

public record DeleteMedicineCommand(Guid Id) : IRequest;

public class DeleteMedicineHandler(
    IMedicineRepository medicineRepository,
    IActivityLogService activityLogService,
    ICurrentUserService currentUser) : IRequestHandler<DeleteMedicineCommand>
{
    public async Task Handle(DeleteMedicineCommand request, CancellationToken ct)
    {
        var medicine = await medicineRepository.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException("Không tìm thấy thuốc.");

        await medicineRepository.DeleteAsync(medicine, ct);

        await activityLogService.LogAsync(
            userId: currentUser.UserId,
            userName: currentUser.UserName,
            userRole: currentUser.UserRole,
            action: ActivityAction.Delete,
            module: ActivityModule.Medicine,
            description: $"Xóa thuốc: {medicine.Name}",
            status: ActivityStatus.Success,
            ipAddress: currentUser.IpAddress,
            targetId: request.Id.ToString(),
            ct: ct);
    }
}
