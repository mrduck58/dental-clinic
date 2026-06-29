using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;

namespace DentalClinic.API.Application.UseCases.Medicines;

public class DeleteMedicineHandler(
    IMedicineRepository medicineRepository,
    IActivityLogService activityLogService,
    ICurrentUserService currentUser)
{
    public async Task HandleAsync(Guid id, CancellationToken ct = default)
    {
        var medicine = await medicineRepository.GetByIdAsync(id, ct)
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
            targetId: id.ToString(),
            ct: ct);
    }
}
