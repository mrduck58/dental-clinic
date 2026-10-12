using DentalClinic.API.Application.DTOs.ClinicalRecords;
using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using MediatR;
using static DentalClinic.API.Application.UseCases.ClinicalRecords.ClinicalRecordMappers;

namespace DentalClinic.API.Application.UseCases.ClinicalRecords;

// ── Request/Command ──────────────────────────────────────────────────────────

public record CreateTreatmentPlanRequest(
    Guid AppointmentId,
    Guid ServiceId,
    decimal? UnitPrice,
    int Quantity,
    string? Teeth,
    string? Notes,
    DateOnly? WarrantyUntil,
    string? ServiceOptionName = null,
    Guid? ServiceOptionId = null,
    int? EstimatedSessionCount = null,
    int? EstimatedDurationMin = null,
    int? EstimatedDurationMax = null,
    string? EstimatedDurationUnit = null,
    DateOnly? EstimatedStartDate = null,
    DateOnly? EstimatedEndDate = null) : IRequest<TreatmentPlanDto>;

public record UpdateTreatmentPlanRequest(
    Guid TreatmentPlanId,
    decimal UnitPrice,
    int Quantity,
    string? Teeth,
    string? Notes,
    DateOnly? WarrantyUntil,
    string? Status,
    int? EstimatedSessionCount = null,
    int? EstimatedDurationMin = null,
    int? EstimatedDurationMax = null,
    string? EstimatedDurationUnit = null,
    DateOnly? EstimatedStartDate = null,
    DateOnly? EstimatedEndDate = null) : IRequest<TreatmentPlanDto>;

public record DeleteTreatmentPlanCommand(Guid TreatmentPlanId) : IRequest;

public record GetPatientTreatmentPlansQuery(Guid PatientId) : IRequest<List<TreatmentPlanDto>>;

public record AddTreatmentSessionRequest(
    Guid TreatmentPlanItemId,
    string Name,
    int DurationMinutes = 30,
    Guid? DentistId = null,
    string? Note = null);

public record AddTreatmentSessionCommand(AddTreatmentSessionRequest Request) : IRequest<TreatmentPlanDto>;

public record RecordTreatmentSessionRequest(
    Guid TreatmentSessionId,
    string Status, // InProgress, Completed, Skipped, Planned
    DateTimeOffset? PerformedAt = null,
    Guid? DentistId = null,
    string? Note = null);

public record RecordTreatmentSessionCommand(RecordTreatmentSessionRequest Request) : IRequest<TreatmentPlanDto>;

public record DeleteTreatmentSessionCommand(Guid TreatmentSessionId) : IRequest<TreatmentPlanDto>;

// Legacy compatibility commands
public record UpdateStepProgressRequest(
    int EntryIndex,
    int Percent,
    string? Note,
    string? StepName = null,
    DateOnly? Date = null);

public record ReorderStepProgressRequest(List<int> Order);

public record AddStepProgressRequest(
    int StepNumber,
    string StepName,
    int Percent,
    DateOnly? Date,
    string? Note);

public record AddStepProgressCommand(Guid TreatmentPlanId, AddStepProgressRequest Request) : IRequest<TreatmentPlanDto>;

public record UpdateStepProgressCommand(Guid TreatmentPlanId, UpdateStepProgressRequest Request) : IRequest<TreatmentPlanDto>;

public record ReorderStepProgressCommand(Guid TreatmentPlanId, ReorderStepProgressRequest Request) : IRequest<TreatmentPlanDto>;

public record DeleteStepProgressCommand(Guid TreatmentPlanId, int EntryIndex) : IRequest<TreatmentPlanDto>;

// ── Handlers ─────────────────────────────────────────────────────────────────

public class CreateTreatmentPlanHandler(
    IAppointmentRepository appointmentRepository,
    IServiceRepository serviceRepository,
    ITreatmentPlanRepository treatmentPlanRepository,
    ITreatmentProcedureRepository treatmentProcedureRepository,
    TreatmentPlanQueryHelper queryHelper,
    IPatientRepository patientRepository,
    INotificationService notificationService) : IRequestHandler<CreateTreatmentPlanRequest, TreatmentPlanDto>
{
    public async Task<TreatmentPlanDto> Handle(CreateTreatmentPlanRequest request, CancellationToken ct)
    {
        var appointment = await appointmentRepository.GetByIdAsync(request.AppointmentId, ct)
            ?? throw new NotFoundException("Không tìm thấy lịch hẹn.");

        if (appointment.Status is not (AppointmentStatus.InProgress or AppointmentStatus.PendingPayment or AppointmentStatus.Completed))
            throw new ValidationException("Chỉ có thể thêm liệu trình khi buổi hẹn đang khám hoặc đã kết thúc điều trị.");

        var service = await serviceRepository.GetByIdAsync(request.ServiceId, ct)
            ?? throw new NotFoundException("Không tìm thấy dịch vụ.");

        var teeth = NormalizeText(request.Teeth);
        var optionName = NormalizeText(request.ServiceOptionName);

        // Tạo TreatmentPlan (Master plan)
        var treatmentPlan = TreatmentPlan.Create(
            appointment.PatientId,
            appointment.DentistId,
            title: service.Name,
            notes: NormalizeText(request.Notes));

        DurationUnit? durationUnit = null;
        if (!string.IsNullOrWhiteSpace(request.EstimatedDurationUnit)
            && Enum.TryParse<DurationUnit>(request.EstimatedDurationUnit, ignoreCase: true, out var parsedUnit))
        {
            durationUnit = parsedUnit;
        }

        // Tạo TreatmentPlanItem (dịch vụ chỉ định)
        var item = TreatmentPlanItem.Create(
            treatmentPlan.Id,
            service.Id,
            request.UnitPrice ?? service.Price,
            request.Quantity,
            teeth,
            NormalizeText(request.Notes),
            request.WarrantyUntil,
            request.ServiceOptionId,
            optionName,
            request.EstimatedSessionCount,
            request.EstimatedDurationMin,
            request.EstimatedDurationMax,
            durationUnit,
            request.EstimatedStartDate,
            request.EstimatedEndDate);

        // Tự động sinh TreatmentPlanSteps từ TreatmentProcedures chuẩn của dịch vụ
        var procedures = (await treatmentProcedureRepository.GetByServiceIdAsync(service.Id, ct)).ToList();
        if (procedures.Count > 0)
        {
            foreach (var proc in procedures.OrderBy(p => p.StepNumber))
            {
                var step = TreatmentPlanStep.Create(
                    item.Id,
                    proc.StepNumber,
                    proc.Name,
                    proc.EstimatedSessionCount > 0 ? proc.EstimatedSessionCount : 1,
                    proc.DurationMinutes > 0 ? proc.DurationMinutes : 30,
                    proc.Id,
                    proc.Description);
                item.Steps.Add(step);
            }
        }
        else
        {
            // Nếu dịch vụ chưa có bước mẫu, tạo 1 step mặc định
            var defaultStep = TreatmentPlanStep.Create(
                item.Id,
                1,
                service.Name,
                item.EstimatedSessionCount ?? 1,
                service.DurationMinutes > 0 ? service.DurationMinutes : 30);
            item.Steps.Add(defaultStep);
        }

        treatmentPlan.Items.Add(item);
        await treatmentPlanRepository.AddAsync(treatmentPlan, ct);

        // Báo cho bệnh nhân có kế hoạch điều trị mới (nếu tài khoản có liên kết User).
        var patient = await patientRepository.GetByIdAsync(appointment.PatientId, ct);
        if (patient != null && patient.UserId != Guid.Empty)
        {
            var patientUserId = patient.UserId;
            await notificationService.CreateAsync(new CreateNotificationRequest(
                UserId: patientUserId,
                Type: NotificationType.Service,
                Priority: NotificationPriority.Medium,
                Title: "Kế hoạch điều trị mới",
                Body: $"Bác sĩ đã lập kế hoạch điều trị: {service.Name}. Xem chi tiết trong hồ sơ khám bệnh.",
                RelatedEntityType: "TreatmentPlan",
                RelatedEntityId: treatmentPlan.Id.ToString()), ct);
        }

        return await queryHelper.LoadDtoAsync(treatmentPlan.Id, ct);
    }
}

public class UpdateTreatmentPlanHandler(
    ITreatmentPlanRepository treatmentPlanRepository,
    TreatmentPlanQueryHelper queryHelper) : IRequestHandler<UpdateTreatmentPlanRequest, TreatmentPlanDto>
{
    public async Task<TreatmentPlanDto> Handle(UpdateTreatmentPlanRequest request, CancellationToken ct)
    {
        var treatmentPlan = await treatmentPlanRepository.GetByIdWithDetailsAsync(request.TreatmentPlanId, ct)
            ?? throw new NotFoundException("Không tìm thấy liệu trình điều trị.");

        var item = treatmentPlan.Items.FirstOrDefault();
        if (item != null)
        {
            var amountPaid = await queryHelper.GetAmountPaidAsync(treatmentPlan.Id, ct);
            if (request.UnitPrice * Math.Max(1, request.Quantity) < amountPaid)
                throw new ValidationException("Tổng chi phí mới không được nhỏ hơn số tiền đã thu.");

            DurationUnit? durationUnit = item.EstimatedDurationUnit;
            if (!string.IsNullOrWhiteSpace(request.EstimatedDurationUnit))
            {
                if (Enum.TryParse<DurationUnit>(request.EstimatedDurationUnit, ignoreCase: true, out var parsedUnit))
                    durationUnit = parsedUnit;
            }

            item.Update(
                request.UnitPrice,
                request.Quantity,
                NormalizeText(request.Teeth),
                NormalizeText(request.Notes),
                request.WarrantyUntil,
                request.EstimatedSessionCount ?? item.EstimatedSessionCount,
                request.EstimatedDurationMin ?? item.EstimatedDurationMin,
                request.EstimatedDurationMax ?? item.EstimatedDurationMax,
                durationUnit,
                request.EstimatedStartDate ?? item.EstimatedStartDate,
                request.EstimatedEndDate ?? item.EstimatedEndDate);

            await treatmentPlanRepository.UpdateItemAsync(item, ct);
        }

        treatmentPlan.Update(treatmentPlan.Title, NormalizeText(request.Notes));

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (!Enum.TryParse<TreatmentPlanStatus>(request.Status, ignoreCase: true, out var status))
                throw new ValidationException("Trạng thái liệu trình không hợp lệ.");

            if (status == TreatmentPlanStatus.Completed)
                throw new ValidationException("Không thể tự chuyển trạng thái thành Hoàn thành. Trạng thái này được hệ thống tự tính dựa trên tiến độ các bước.");

            if (status == TreatmentPlanStatus.Cancelled
                && treatmentPlan.Status != TreatmentPlanStatus.Cancelled
                && await queryHelper.IsInvoicedAsync(treatmentPlan.Id, ct))
                throw new ValidationException("Dịch vụ này đã được xuất hóa đơn nên không thể hủy. Cần lễ tân hoàn/hủy hóa đơn trước.");

            treatmentPlan.SetStatus(status);
        }

        await treatmentPlanRepository.UpdateAsync(treatmentPlan, ct);

        return await queryHelper.LoadDtoAsync(treatmentPlan.Id, ct);
    }
}

public class DeleteTreatmentPlanHandler(
    ITreatmentPlanRepository treatmentPlanRepository,
    IMaterialRequestRepository materialRequestRepository,
    TreatmentPlanQueryHelper queryHelper) : IRequestHandler<DeleteTreatmentPlanCommand>
{
    public async Task Handle(DeleteTreatmentPlanCommand command, CancellationToken ct)
    {
        var treatmentPlan = await treatmentPlanRepository.GetByIdAsync(command.TreatmentPlanId, ct)
            ?? throw new NotFoundException("Không tìm thấy liệu trình điều trị.");

        if (await queryHelper.IsInvoicedAsync(treatmentPlan.Id, ct))
            throw new ValidationException("Dịch vụ này đã được xuất hóa đơn nên không thể xóa khỏi liệu trình. Cần lễ tân hoàn/hủy hóa đơn trước.");

        var linkedRequests = await materialRequestRepository.GetByTreatmentPlanIdAsync(treatmentPlan.Id, ct);
        if (linkedRequests.Any(r => r.Status != MaterialRequestStatus.Pending))
            throw new ValidationException(
                "Dịch vụ này có yêu cầu vật tư đã đặt hàng hoặc đã nhập kho — cần xử lý xong yêu cầu vật tư đó trước khi xóa dịch vụ.");

        foreach (var pendingRequest in linkedRequests)
            await materialRequestRepository.DeleteAsync(pendingRequest, ct);

        await treatmentPlanRepository.DeleteAsync(treatmentPlan, ct);
    }
}

public class GetPatientTreatmentPlansHandler(
    ITreatmentPlanRepository treatmentPlanRepository,
    TreatmentPlanQueryHelper queryHelper) : IRequestHandler<GetPatientTreatmentPlansQuery, List<TreatmentPlanDto>>
{
    public async Task<List<TreatmentPlanDto>> Handle(GetPatientTreatmentPlansQuery request, CancellationToken ct)
    {
        var plans = await treatmentPlanRepository.GetByPatientIdAsync(request.PatientId, ct);

        var planIds = plans.Select(p => p.Id).ToList();
        var paidMap = await queryHelper.GetAmountPaidMapAsync(planIds, ct);
        var invoicedSet = await queryHelper.GetInvoicedPlanIdsAsync(planIds, ct);

        return plans.Select(p => ClinicalRecordMappers.ToDto(
            p,
            paidMap.GetValueOrDefault(p.Id, 0m),
            invoicedSet.Contains(p.Id))).ToList();
    }
}

public class AddStepProgressHandler(
    ITreatmentPlanRepository treatmentPlanRepository,
    TreatmentPlanQueryHelper queryHelper) : IRequestHandler<AddStepProgressCommand, TreatmentPlanDto>
{
    public async Task<TreatmentPlanDto> Handle(AddStepProgressCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Request.StepName))
            throw new ValidationException("Tên bước điều trị không được để trống.");

        var treatmentPlan = await treatmentPlanRepository.GetByIdWithDetailsAsync(command.TreatmentPlanId, ct)
            ?? throw new NotFoundException("Không tìm thấy liệu trình điều trị.");

        if (!await queryHelper.HasActiveVisitAsync(treatmentPlan.PatientId, ct))
            throw new ValidationException("Chỉ có thể ghi nhận quá trình điều trị khi buổi hẹn đang khám hoặc đã kết thúc điều trị.");

        var item = treatmentPlan.Items.FirstOrDefault();
        if (item == null)
            throw new NotFoundException("Kế hoạch chưa có dịch vụ chỉ định.");

        var step = item.Steps.FirstOrDefault(s => s.StepNumber == command.Request.StepNumber)
            ?? item.Steps.FirstOrDefault(s => s.Name.Equals(command.Request.StepName.Trim(), StringComparison.OrdinalIgnoreCase));

        if (step == null)
        {
            var nextNum = item.Steps.Count > 0 ? item.Steps.Max(s => s.StepNumber) + 1 : 1;
            step = TreatmentPlanStep.Create(
                item.Id,
                command.Request.StepNumber > 0 ? command.Request.StepNumber : nextNum,
                command.Request.StepName.Trim(),
                1,
                30,
                null,
                null,
                NormalizeText(command.Request.Note));
            item.Steps.Add(step);
            await treatmentPlanRepository.AddStepAsync(step, ct);
        }

        step.SetPercent(command.Request.Percent);

        // Tạo TreatmentSession cho buổi ghi nhận này
        var nextSessionNum = step.Sessions.Count > 0 ? step.Sessions.Max(s => s.SessionNumber) + 1 : 1;
        var session = TreatmentSession.Create(
            step.Id,
            nextSessionNum,
            step.Name,
            30,
            treatmentPlan.DentistId,
            NormalizeText(command.Request.Note));

        var sessionStatus = command.Request.Percent >= 100
            ? TreatmentSessionStatus.Completed
            : (command.Request.Percent > 0 ? TreatmentSessionStatus.InProgress : TreatmentSessionStatus.Planned);

        session.SetStatus(
            sessionStatus,
            performedAt: command.Request.Date.HasValue ? new DateTimeOffset(command.Request.Date.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero) : DateTimeOffset.UtcNow,
            percent: command.Request.Percent);

        step.Sessions.Add(session);
        await treatmentPlanRepository.AddSessionAsync(session, ct);

        await queryHelper.SyncStatusWithProgressAsync(item, ct);
        await treatmentPlanRepository.UpdateAsync(treatmentPlan, ct);

        return await queryHelper.LoadDtoAsync(treatmentPlan.Id, ct);
    }
}

public class UpdateStepProgressHandler(
    ITreatmentPlanRepository treatmentPlanRepository,
    TreatmentPlanQueryHelper queryHelper) : IRequestHandler<UpdateStepProgressCommand, TreatmentPlanDto>
{
    public async Task<TreatmentPlanDto> Handle(UpdateStepProgressCommand command, CancellationToken ct)
    {
        var treatmentPlan = await treatmentPlanRepository.GetByIdWithDetailsAsync(command.TreatmentPlanId, ct)
            ?? throw new NotFoundException("Không tìm thấy liệu trình điều trị.");

        if (!await queryHelper.HasActiveVisitAsync(treatmentPlan.PatientId, ct))
            throw new ValidationException("Chỉ có thể sửa quá trình điều trị khi buổi hẹn đang khám hoặc đã kết thúc điều trị.");

        var item = treatmentPlan.Items.FirstOrDefault();
        if (item == null || command.Request.EntryIndex < 0 || command.Request.EntryIndex >= item.Steps.Count)
            throw new ValidationException("Không tìm thấy bước điều trị cần sửa.");

        var sortedSteps = item.Steps.OrderBy(s => s.StepNumber).ToList();
        var step = sortedSteps[command.Request.EntryIndex];

        step.Update(
            string.IsNullOrWhiteSpace(command.Request.StepName) ? step.Name : command.Request.StepName.Trim(),
            step.EstimatedSessionCount,
            step.DurationMinutes,
            step.Description,
            NormalizeText(command.Request.Note));

        step.SetPercent(command.Request.Percent);

        await queryHelper.SyncStatusWithProgressAsync(item, ct);
        await treatmentPlanRepository.UpdateAsync(treatmentPlan, ct);

        return await queryHelper.LoadDtoAsync(treatmentPlan.Id, ct);
    }
}

public class ReorderStepProgressHandler(
    ITreatmentPlanRepository treatmentPlanRepository,
    TreatmentPlanQueryHelper queryHelper) : IRequestHandler<ReorderStepProgressCommand, TreatmentPlanDto>
{
    public async Task<TreatmentPlanDto> Handle(ReorderStepProgressCommand command, CancellationToken ct)
    {
        var treatmentPlan = await treatmentPlanRepository.GetByIdWithDetailsAsync(command.TreatmentPlanId, ct)
            ?? throw new NotFoundException("Không tìm thấy liệu trình điều trị.");

        if (!await queryHelper.HasActiveVisitAsync(treatmentPlan.PatientId, ct))
            throw new ValidationException("Chỉ có thể sắp xếp lại quá trình điều trị khi buổi hẹn đang khám hoặc đã kết thúc điều trị.");

        var item = treatmentPlan.Items.FirstOrDefault();
        if (item == null)
            throw new NotFoundException("Kế hoạch chưa có dịch vụ chỉ định.");

        var sortedSteps = item.Steps.OrderBy(s => s.StepNumber).ToList();
        var count = sortedSteps.Count;
        if (command.Request.Order == null
            || command.Request.Order.Count != count
            || command.Request.Order.Distinct().Count() != count
            || command.Request.Order.Any(idx => idx < 0 || idx >= count))
        {
            throw new ValidationException("Thứ tự sắp xếp không hợp lệ.");
        }

        for (int i = 0; i < count; i++)
        {
            var oldIdx = command.Request.Order[i];
            sortedSteps[oldIdx].SetStepNumber(i + 1);
        }

        await treatmentPlanRepository.UpdateAsync(treatmentPlan, ct);

        return await queryHelper.LoadDtoAsync(treatmentPlan.Id, ct);
    }
}

public class DeleteStepProgressHandler(
    ITreatmentPlanRepository treatmentPlanRepository,
    ITreatmentSupplyUsageRepository treatmentSupplyUsageRepository,
    ISupplyItemRepository supplyItemRepository,
    ISupplyTransactionRepository supplyTransactionRepository,
    TreatmentPlanQueryHelper queryHelper,
    IActivityLogService activityLogService) : IRequestHandler<DeleteStepProgressCommand, TreatmentPlanDto>
{
    public async Task<TreatmentPlanDto> Handle(DeleteStepProgressCommand command, CancellationToken ct)
    {
        var treatmentPlan = await treatmentPlanRepository.GetByIdWithDetailsAsync(command.TreatmentPlanId, ct)
            ?? throw new NotFoundException("Không tìm thấy liệu trình điều trị.");

        if (!await queryHelper.HasActiveVisitAsync(treatmentPlan.PatientId, ct))
            throw new ValidationException("Chỉ có thể xóa quá trình điều trị khi buổi hẹn đang khám hoặc đã kết thúc điều trị.");

        var item = treatmentPlan.Items.FirstOrDefault();
        if (item == null || command.EntryIndex < 0 || command.EntryIndex >= item.Steps.Count)
            throw new ValidationException("Không tìm thấy bước điều trị cần xóa.");

        var sortedSteps = item.Steps.OrderBy(s => s.StepNumber).ToList();
        var step = sortedSteps[command.EntryIndex];

        foreach (var session in step.Sessions)
        {
            var activeUsages = await treatmentSupplyUsageRepository.GetActiveByStepEntryIdAsync(treatmentPlan.Id, session.Id, ct);
            foreach (var usage in activeUsages)
            {
                usage.MarkReversed();

                var supplyItem = await supplyItemRepository.GetByIdAsync(usage.SupplyItemId, ct);
                if (supplyItem != null)
                {
                    supplyItem.AdjustQuantity(usage.Quantity);
                    await supplyItemRepository.UpdateAsync(supplyItem, ct);

                    var tx = SupplyTransaction.Create(
                        usage.SupplyItemId,
                        "import",
                        usage.Quantity,
                        "Hoàn trả vật tư do xóa bước điều trị",
                        usage.CreatedBy,
                        usage.UnitCostAtUsage);
                    await supplyTransactionRepository.AddAsync(tx, ct);
                }
            }
        }

        item.Steps.Remove(step);
        await treatmentPlanRepository.DeleteStepAsync(step, ct);

        var remainingSteps = item.Steps.OrderBy(s => s.StepNumber).ToList();
        for (int i = 0; i < remainingSteps.Count; i++)
        {
            remainingSteps[i].SetStepNumber(i + 1);
        }

        await queryHelper.SyncStatusWithProgressAsync(item, ct);
        await treatmentPlanRepository.UpdateAsync(treatmentPlan, ct);

        return await queryHelper.LoadDtoAsync(treatmentPlan.Id, ct);
    }
}
