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
// Hình dạng JSON body giữ NGUYÊN như trước khi tách god-handler TreatmentPlanHandler.

public record CreateTreatmentPlanRequest(
    Guid AppointmentId,
    Guid ServiceId,
    decimal? UnitPrice,
    int Quantity,
    string? Teeth,
    string? Notes,
    DateOnly? WarrantyUntil,
    string? ServiceOptionName = null) : IRequest<TreatmentPlanDto>;

public record UpdateTreatmentPlanRequest(
    Guid TreatmentPlanId,
    decimal UnitPrice,
    int Quantity,
    string? Teeth,
    string? Notes,
    DateOnly? WarrantyUntil,
    string? Status) : IRequest<TreatmentPlanDto>;

public record DeleteTreatmentPlanCommand(Guid TreatmentPlanId) : IRequest;

public record GetPatientTreatmentPlansQuery(Guid PatientId) : IRequest<List<TreatmentPlanDto>>;

/// <summary>Sửa một mục trong nhật ký điều trị — EntryIndex là vị trí trong mảng StepProgressJson.</summary>
public record UpdateStepProgressRequest(
    int EntryIndex,
    int Percent,
    string? Note,
    string? StepName = null,
    DateOnly? Date = null);

/// <summary>Sắp xếp lại nhật ký điều trị — Order là danh sách index gốc theo thứ tự mới.</summary>
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

        var treatmentPlan = TreatmentPlan.Create(
            appointment.PatientId,
            appointment.DentistId,
            appointment.Id,
            service.Id,
            request.UnitPrice ?? service.Price,
            request.Quantity,
            NormalizeText(request.Teeth),
            NormalizeText(request.Notes),
            request.WarrantyUntil,
            NormalizeText(request.ServiceOptionName));

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
        var treatmentPlan = await treatmentPlanRepository.GetByIdAsync(request.TreatmentPlanId, ct)
            ?? throw new NotFoundException("Không tìm thấy liệu trình điều trị.");

        var amountPaid = await queryHelper.GetAmountPaidAsync(treatmentPlan.Id, ct);
        if (request.UnitPrice * Math.Max(1, request.Quantity) < amountPaid)
            throw new ValidationException("Tổng chi phí mới không được nhỏ hơn số tiền đã thu.");

        treatmentPlan.Update(
            request.UnitPrice,
            request.Quantity,
            NormalizeText(request.Teeth),
            NormalizeText(request.Notes),
            request.WarrantyUntil);

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (!Enum.TryParse<TreatmentPlanStatus>(request.Status, ignoreCase: true, out var status))
                throw new ValidationException("Trạng thái liệu trình không hợp lệ.");

            // Chờ thực hiện / đang thực hiện / hoàn thành là KẾT QUẢ tính từ tiến độ các bước quy trình
            // (SyncStatusWithProgressAsync) — không ai được đặt tay, tránh đánh dấu "hoàn thành" khi
            // bệnh nhân còn đang điều trị dở. Chỉ HỦY là quyết định hành chính nên vẫn nhận.
            if (status != TreatmentPlanStatus.Cancelled && status != treatmentPlan.Status)
                throw new ValidationException(
                    "Trạng thái điều trị do hệ thống tự cập nhật theo tiến độ các bước quy trình — không đặt thủ công.");

            // Hủy liệu trình = loại nó khỏi liệu trình/tổng chi phí → cũng bị chặn khi đã xuất hóa đơn.
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
    TreatmentPlanQueryHelper queryHelper) : IRequestHandler<DeleteTreatmentPlanCommand>
{
    public async Task Handle(DeleteTreatmentPlanCommand command, CancellationToken ct)
    {
        var treatmentPlan = await treatmentPlanRepository.GetByIdAsync(command.TreatmentPlanId, ct)
            ?? throw new NotFoundException("Không tìm thấy liệu trình điều trị.");

        if (await queryHelper.IsInvoicedAsync(treatmentPlan.Id, ct))
            throw new ValidationException("Dịch vụ này đã được xuất hóa đơn nên không thể xóa khỏi liệu trình. Cần lễ tân hoàn/hủy hóa đơn trước.");

        await treatmentPlanRepository.DeleteAsync(treatmentPlan, ct);
    }
}

/// <summary>Tất cả liệu trình của một bệnh nhân (mọi buổi hẹn), kèm số tiền đã thu.</summary>
public class GetPatientTreatmentPlansHandler(
    ITreatmentPlanRepository treatmentPlanRepository,
    TreatmentPlanQueryHelper queryHelper) : IRequestHandler<GetPatientTreatmentPlansQuery, List<TreatmentPlanDto>>
{
    public async Task<List<TreatmentPlanDto>> Handle(GetPatientTreatmentPlansQuery request, CancellationToken ct)
    {
        var plans = await treatmentPlanRepository.GetByPatientIdAsync(request.PatientId, ct);

        var planIds = plans.Select(p => p.Id).ToList();
        var paidMap = await queryHelper.GetAmountPaidMapAsync(planIds, ct);
        var invoicedIds = await queryHelper.GetInvoicedPlanIdsAsync(planIds, ct);
        var stepMap = await queryHelper.GetProcedureStepNumbersMapAsync(plans.Select(p => p.ServiceId).ToList(), ct);

        return plans
            .Select(p => ToDto(
                p,
                paidMap.GetValueOrDefault(p.Id),
                invoicedIds.Contains(p.Id),
                stepMap.GetValueOrDefault(p.ServiceId)))
            .ToList();
    }
}

/// <summary>Ghi nhận một bước quy trình đã thực hiện vào nhật ký điều trị của liệu trình.</summary>
public class AddStepProgressHandler(
    ITreatmentPlanRepository treatmentPlanRepository,
    TreatmentPlanQueryHelper queryHelper) : IRequestHandler<AddStepProgressCommand, TreatmentPlanDto>
{
    public async Task<TreatmentPlanDto> Handle(AddStepProgressCommand command, CancellationToken ct)
    {
        var treatmentPlanId = command.TreatmentPlanId;
        var request = command.Request;

        var treatmentPlan = await treatmentPlanRepository.GetByIdWithDentistAsync(treatmentPlanId, ct)
            ?? throw new NotFoundException("Không tìm thấy liệu trình điều trị.");

        if (string.IsNullOrWhiteSpace(request.StepName))
            throw new ValidationException("Tên bước điều trị không được để trống.");

        // Chỉ ghi nhận quá trình khi bệnh nhân đang trong buổi khám (bác sĩ đã bấm "Bắt đầu khám")
        if (!await queryHelper.HasActiveVisitAsync(treatmentPlan.PatientId, ct))
            throw new ValidationException("Chỉ có thể ghi nhận quá trình điều trị khi buổi hẹn đang khám hoặc đã kết thúc điều trị.");

        var entries = ParseStepProgress(treatmentPlan.StepProgressJson);
        entries.Add(new StepProgressEntryDto
        {
            Id = Guid.NewGuid(),
            StepNumber = request.StepNumber,
            StepName = request.StepName.Trim(),
            Percent = Math.Clamp(request.Percent, 0, 100),
            Date = request.Date ?? DateOnly.FromDateTime(DateTime.Today),
            DentistName = treatmentPlan.Dentist.FullName,
            Note = NormalizeText(request.Note)
        });

        treatmentPlan.UpdateStepProgress(SerializeStepProgress(entries));

        // Trạng thái bám theo tiến độ: có bước dở dang → đang thực hiện, đủ 100% mọi bước → hoàn thành.
        await queryHelper.SyncStatusWithProgressAsync(treatmentPlan, entries, ct);

        await treatmentPlanRepository.UpdateAsync(treatmentPlan, ct);

        return await queryHelper.LoadDtoAsync(treatmentPlan.Id, ct);
    }
}

/// <summary>Sửa một mục đã ghi trong nhật ký điều trị (theo vị trí trong danh sách).</summary>
public class UpdateStepProgressHandler(
    ITreatmentPlanRepository treatmentPlanRepository,
    TreatmentPlanQueryHelper queryHelper) : IRequestHandler<UpdateStepProgressCommand, TreatmentPlanDto>
{
    public async Task<TreatmentPlanDto> Handle(UpdateStepProgressCommand command, CancellationToken ct)
    {
        var treatmentPlanId = command.TreatmentPlanId;
        var request = command.Request;

        var treatmentPlan = await treatmentPlanRepository.GetByIdAsync(treatmentPlanId, ct)
            ?? throw new NotFoundException("Không tìm thấy liệu trình điều trị.");

        if (!await queryHelper.HasActiveVisitAsync(treatmentPlan.PatientId, ct))
            throw new ValidationException("Chỉ có thể sửa quá trình điều trị khi buổi hẹn đang khám hoặc đã kết thúc điều trị.");

        var entries = ParseStepProgress(treatmentPlan.StepProgressJson);
        if (request.EntryIndex < 0 || request.EntryIndex >= entries.Count)
            throw new ValidationException("Không tìm thấy bước điều trị cần sửa.");

        entries[request.EntryIndex].Percent = Math.Clamp(request.Percent, 0, 100);
        entries[request.EntryIndex].Note = NormalizeText(request.Note);
        if (!string.IsNullOrWhiteSpace(request.StepName))
            entries[request.EntryIndex].StepName = request.StepName.Trim();
        if (request.Date.HasValue)
            entries[request.EntryIndex].Date = request.Date.Value;

        treatmentPlan.UpdateStepProgress(SerializeStepProgress(entries));
        await queryHelper.SyncStatusWithProgressAsync(treatmentPlan, entries, ct);
        await treatmentPlanRepository.UpdateAsync(treatmentPlan, ct);

        return await queryHelper.LoadDtoAsync(treatmentPlan.Id, ct);
    }
}

/// <summary>Sắp xếp lại thứ tự các mục trong nhật ký điều trị. Order là hoán vị các index gốc theo thứ tự mới.</summary>
public class ReorderStepProgressHandler(
    ITreatmentPlanRepository treatmentPlanRepository,
    TreatmentPlanQueryHelper queryHelper) : IRequestHandler<ReorderStepProgressCommand, TreatmentPlanDto>
{
    public async Task<TreatmentPlanDto> Handle(ReorderStepProgressCommand command, CancellationToken ct)
    {
        var treatmentPlanId = command.TreatmentPlanId;
        var request = command.Request;

        var treatmentPlan = await treatmentPlanRepository.GetByIdAsync(treatmentPlanId, ct)
            ?? throw new NotFoundException("Không tìm thấy liệu trình điều trị.");

        if (!await queryHelper.HasActiveVisitAsync(treatmentPlan.PatientId, ct))
            throw new ValidationException("Chỉ có thể đổi thứ tự quá trình điều trị khi buổi hẹn đang khám hoặc đã kết thúc điều trị.");

        var entries = ParseStepProgress(treatmentPlan.StepProgressJson);
        var order = request.Order;
        // Order phải là một hoán vị đầy đủ 0..n-1 (không thiếu, không trùng, không lệch)
        if (order.Count != entries.Count ||
            order.Distinct().Count() != entries.Count ||
            order.Any(i => i < 0 || i >= entries.Count))
            throw new ValidationException("Thứ tự quá trình điều trị không hợp lệ.");

        var reordered = order.Select(i => entries[i]).ToList();
        treatmentPlan.UpdateStepProgress(SerializeStepProgress(reordered));
        await treatmentPlanRepository.UpdateAsync(treatmentPlan, ct);

        return await queryHelper.LoadDtoAsync(treatmentPlan.Id, ct);
    }
}

/// <summary>Xóa một mục đã ghi trong nhật ký điều trị (theo vị trí trong danh sách). Nếu mục này có vật tư
/// đã ghi nhận tiêu hao gắn theo (StepEntryId) thì tự động hoàn lại đúng số lượng vào kho.</summary>
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
        var treatmentPlanId = command.TreatmentPlanId;
        var entryIndex = command.EntryIndex;

        var treatmentPlan = await treatmentPlanRepository.GetByIdWithDentistAsync(treatmentPlanId, ct)
            ?? throw new NotFoundException("Không tìm thấy liệu trình điều trị.");

        if (!await queryHelper.HasActiveVisitAsync(treatmentPlan.PatientId, ct))
            throw new ValidationException("Chỉ có thể xóa quá trình điều trị khi buổi hẹn đang khám hoặc đã kết thúc điều trị.");

        var entries = ParseStepProgress(treatmentPlan.StepProgressJson);
        if (entryIndex < 0 || entryIndex >= entries.Count)
            throw new ValidationException("Không tìm thấy bước điều trị cần xóa.");

        var deletedEntryId = entries[entryIndex].Id;
        entries.RemoveAt(entryIndex);
        treatmentPlan.UpdateStepProgress(entries.Count == 0 ? null : SerializeStepProgress(entries));
        // Xóa bớt bước có thể kéo dịch vụ khỏi trạng thái hoàn thành → tính lại.
        await queryHelper.SyncStatusWithProgressAsync(treatmentPlan, entries, ct);
        await treatmentPlanRepository.UpdateAsync(treatmentPlan, ct);

        // Hoàn kho vật tư đã ghi nhận cùng bước vừa xóa (nếu có) — dữ liệu cũ trước khi có StepEntryId sẽ
        // có Id rỗng nên không khớp được dòng nào, bỏ qua an toàn.
        if (deletedEntryId != Guid.Empty)
            await ReverseSupplyUsageAsync(treatmentPlan, deletedEntryId, ct);

        return await queryHelper.LoadDtoAsync(treatmentPlan.Id, ct);
    }

    private async Task ReverseSupplyUsageAsync(TreatmentPlan treatmentPlan, Guid stepEntryId, CancellationToken ct)
    {
        var usagesToReverse = await treatmentSupplyUsageRepository.GetActiveByStepEntryIdAsync(treatmentPlan.Id, stepEntryId, ct);
        if (usagesToReverse.Count == 0) return;

        var dentistName = treatmentPlan.Dentist.FullName;

        foreach (var usage in usagesToReverse)
        {
            var supplyItem = await supplyItemRepository.GetByIdAsync(usage.SupplyItemId, ct);
            if (supplyItem is null) continue; // vật tư đã bị xóa khỏi kho — không còn gì để hoàn lại

            supplyItem.AdjustQuantity(usage.Quantity);
            usage.MarkReversed();

            var tx = SupplyTransaction.Create(
                supplyItem.Id, "import", usage.Quantity,
                $"Hoàn kho (xóa quá trình điều trị) · BS {dentistName}", dentistName);
            // AddAsync tự SaveChanges — flush luôn AdjustQuantity/MarkReversed đang pending ở trên cùng lượt.
            await supplyTransactionRepository.AddAsync(tx, ct);

            await activityLogService.LogAsync(
                userId: null,
                userName: dentistName,
                userRole: "Dentist",
                action: ActivityAction.Create,
                module: ActivityModule.Inventory,
                description: $"hoàn kho (xóa quá trình điều trị): {supplyItem.Name} x{usage.Quantity}",
                status: ActivityStatus.Success,
                targetId: usage.Id.ToString(),
                ct: ct);
        }
    }
}
