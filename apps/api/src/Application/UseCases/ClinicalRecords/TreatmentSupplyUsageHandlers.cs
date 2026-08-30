using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using MediatR;

namespace DentalClinic.API.Application.UseCases.ClinicalRecords;

public record RecordSupplyUsageItemInput(Guid SupplyItemId, int Quantity);

public record RecordTreatmentSupplyUsageRequest(List<RecordSupplyUsageItemInput> Items, Guid? StepEntryId = null, Guid? TreatmentSessionId = null);

public record RecordTreatmentSupplyUsageCommand(Guid TreatmentPlanId, RecordTreatmentSupplyUsageRequest Request)
    : IRequest<List<TreatmentSupplyUsageDto>>;

public record GetTreatmentSupplyUsageQuery(Guid TreatmentPlanId) : IRequest<List<TreatmentSupplyUsageDto>>;

public class TreatmentSupplyUsageDto
{
    public Guid Id { get; set; }
    public Guid SupplyItemId { get; set; }
    public string SupplyItemName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitCostAtUsage { get; set; }
    public decimal TotalCost => Quantity * UnitCostAtUsage;
    public DateTimeOffset CreatedAt { get; set; }
    public bool IsReversed { get; set; }
}

public class RecordTreatmentSupplyUsageHandler(
    ITreatmentPlanRepository treatmentPlanRepository,
    ISupplyItemRepository supplyItemRepository,
    ISupplyTransactionRepository supplyTransactionRepository,
    ITreatmentSupplyUsageRepository treatmentSupplyUsageRepository,
    TreatmentPlanQueryHelper queryHelper,
    IActivityLogService activityLogService)
    : IRequestHandler<RecordTreatmentSupplyUsageCommand, List<TreatmentSupplyUsageDto>>,
      IRequestHandler<GetTreatmentSupplyUsageQuery, List<TreatmentSupplyUsageDto>>
{
    public async Task<List<TreatmentSupplyUsageDto>> Handle(RecordTreatmentSupplyUsageCommand command, CancellationToken ct)
    {
        var items = command.Request.Items;
        if (items is not { Count: > 0 })
            throw new ValidationException("Phải chọn ít nhất 1 vật tư đã dùng.");

        if (items.Any(i => i.Quantity <= 0))
            throw new ValidationException("Số lượng vật tư dùng phải lớn hơn 0.");

        if (items.Select(i => i.SupplyItemId).Distinct().Count() != items.Count)
            throw new ValidationException("Không được ghi trùng một vật tư nhiều lần trong cùng lượt.");

        var treatmentPlan = await treatmentPlanRepository.GetByIdWithDentistAsync(command.TreatmentPlanId, ct)
            ?? throw new NotFoundException("Không tìm thấy liệu trình điều trị.");

        if (!await queryHelper.HasActiveVisitAsync(treatmentPlan.PatientId, ct))
            throw new ValidationException("Chỉ có thể ghi nhận vật tư khi buổi hẹn đang khám hoặc đã kết thúc điều trị.");

        var dentistName = treatmentPlan.Dentist?.FullName ?? string.Empty;

        await using var transaction = await treatmentSupplyUsageRepository.BeginTransactionAsync(ct);
        foreach (var line in items)
        {
            var supplyItem = await supplyItemRepository.GetByIdAsync(line.SupplyItemId, ct)
                ?? throw new NotFoundException("Không tìm thấy vật tư.");

            if (line.Quantity > supplyItem.Quantity)
                throw new ValidationException(
                    $"Số lượng dùng ({line.Quantity}) vượt quá tồn kho hiện tại của \"{supplyItem.Name}\" ({supplyItem.Quantity}).");

            supplyItem.AdjustQuantity(-line.Quantity);

            var tx = SupplyTransaction.Create(
                supplyItem.Id, "export", line.Quantity,
                $"Tiêu hao điều trị · BS {dentistName}", dentistName);
            await supplyTransactionRepository.AddAsync(tx, ct);

            var usage = TreatmentSupplyUsage.Create(
                treatmentPlan.Id, supplyItem.Id, line.Quantity, supplyItem.Price ?? 0m, tx.Id, dentistName,
                command.Request.StepEntryId, command.Request.TreatmentSessionId);
            await treatmentSupplyUsageRepository.AddAsync(usage, ct);

            await activityLogService.LogAsync(
                userId: null,
                userName: dentistName,
                userRole: "Dentist",
                action: ActivityAction.Create,
                module: ActivityModule.Inventory,
                description: $"ghi nhận tiêu hao điều trị: {supplyItem.Name} x{line.Quantity}",
                status: ActivityStatus.Success,
                targetId: usage.Id.ToString(),
                ct: ct);
        }

        if (transaction != null)
        {
            await transaction.CommitAsync(ct);
        }

        return await LoadListAsync(treatmentPlan.Id, ct);
    }

    public async Task<List<TreatmentSupplyUsageDto>> Handle(GetTreatmentSupplyUsageQuery request, CancellationToken ct) =>
        await LoadListAsync(request.TreatmentPlanId, ct);

    private async Task<List<TreatmentSupplyUsageDto>> LoadListAsync(Guid treatmentPlanId, CancellationToken ct)
    {
        var rows = await treatmentSupplyUsageRepository.GetByTreatmentPlanIdAsync(treatmentPlanId, ct);
        return rows.Select(u => new TreatmentSupplyUsageDto
        {
            Id = u.Id,
            SupplyItemId = u.SupplyItemId,
            SupplyItemName = u.SupplyItem.Name,
            Unit = u.SupplyItem.Unit,
            Quantity = u.Quantity,
            UnitCostAtUsage = u.UnitCostAtUsage,
            CreatedAt = u.CreatedAt,
            IsReversed = u.IsReversed
        }).ToList();
    }
}
