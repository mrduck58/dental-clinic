using System.Text.Json;
using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Application.UseCases.Appointments;

public record CreateTreatmentPlanRequest(
    Guid AppointmentId,
    Guid ServiceId,
    decimal? UnitPrice,
    int Quantity,
    string? Teeth,
    string? Notes,
    DateOnly? WarrantyUntil);

public record UpdateTreatmentPlanRequest(
    Guid TreatmentPlanId,
    decimal UnitPrice,
    int Quantity,
    string? Teeth,
    string? Notes,
    DateOnly? WarrantyUntil,
    string? Status);

/// <summary>Sửa một mục trong nhật ký điều trị — EntryIndex là vị trí trong mảng StepProgressJson.</summary>
public record UpdateStepProgressRequest(
    int EntryIndex,
    int Percent,
    string? Note,
    string? StepName = null);

/// <summary>Sắp xếp lại nhật ký điều trị — Order là danh sách index gốc theo thứ tự mới.</summary>
public record ReorderStepProgressRequest(List<int> Order);

public record AddStepProgressRequest(
    int StepNumber,
    string StepName,
    int Percent,
    DateOnly? Date,
    string? Note);

public class StepProgressEntryDto
{
    public int StepNumber { get; set; }
    public string StepName { get; set; } = string.Empty;
    public int Percent { get; set; }
    public DateOnly Date { get; set; }
    public string DentistName { get; set; } = string.Empty;
    public string? Note { get; set; }
}

public class TreatmentPlanDto
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Guid DentistId { get; set; }
    public string DentistName { get; set; } = string.Empty;
    public Guid? AppointmentId { get; set; }
    public Guid ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public string? Teeth { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateOnly? WarrantyUntil { get; set; }
    public string? Notes { get; set; }
    public decimal TotalCost { get; set; }
    public decimal AmountPaid { get; set; }
    /// <summary>Đã được xuất hóa đơn (hóa đơn chưa hoàn tiền) — bác sĩ không được xóa/hủy liệu trình này nữa.</summary>
    public bool IsInvoiced { get; set; }
    public List<StepProgressEntryDto> StepProgress { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}

public class TreatmentPlanHandler(
    AppDbContext dbContext,
    IPatientRepository patientRepository,
    INotificationService notificationService)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<TreatmentPlanDto> CreateAsync(CreateTreatmentPlanRequest request, CancellationToken ct = default)
    {
        var appointment = await dbContext.Appointments
            .FirstOrDefaultAsync(a => a.Id == request.AppointmentId, ct)
            ?? throw new NotFoundException("Không tìm thấy lịch hẹn.");

        if (appointment.Status is not (AppointmentStatus.InProgress or AppointmentStatus.PendingPayment or AppointmentStatus.Completed))
            throw new ValidationException("Chỉ có thể thêm liệu trình khi buổi hẹn đang khám hoặc đã kết thúc điều trị.");

        var service = await dbContext.Services
            .FirstOrDefaultAsync(s => s.Id == request.ServiceId, ct)
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
            request.WarrantyUntil);

        dbContext.TreatmentPlans.Add(treatmentPlan);
        await dbContext.SaveChangesAsync(ct);

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

        return await LoadDtoAsync(treatmentPlan.Id, ct);
    }

    public async Task<TreatmentPlanDto> UpdateAsync(UpdateTreatmentPlanRequest request, CancellationToken ct = default)
    {
        var treatmentPlan = await dbContext.TreatmentPlans
            .FirstOrDefaultAsync(tp => tp.Id == request.TreatmentPlanId, ct)
            ?? throw new NotFoundException("Không tìm thấy liệu trình điều trị.");

        var amountPaid = await GetAmountPaidAsync(treatmentPlan.Id, ct);
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

            // Hủy liệu trình = loại nó khỏi liệu trình/tổng chi phí → cũng bị chặn khi đã xuất hóa đơn.
            if (status == TreatmentPlanStatus.Cancelled
                && treatmentPlan.Status != TreatmentPlanStatus.Cancelled
                && await IsInvoicedAsync(treatmentPlan.Id, ct))
                throw new ValidationException("Dịch vụ này đã được xuất hóa đơn nên không thể hủy. Cần lễ tân hoàn/hủy hóa đơn trước.");

            treatmentPlan.SetStatus(status);
        }

        await dbContext.SaveChangesAsync(ct);

        return await LoadDtoAsync(treatmentPlan.Id, ct);
    }

    public async Task DeleteAsync(Guid treatmentPlanId, CancellationToken ct = default)
    {
        var treatmentPlan = await dbContext.TreatmentPlans
            .FirstOrDefaultAsync(tp => tp.Id == treatmentPlanId, ct)
            ?? throw new NotFoundException("Không tìm thấy liệu trình điều trị.");

        if (await IsInvoicedAsync(treatmentPlanId, ct))
            throw new ValidationException("Dịch vụ này đã được xuất hóa đơn nên không thể xóa khỏi liệu trình. Cần lễ tân hoàn/hủy hóa đơn trước.");

        dbContext.TreatmentPlans.Remove(treatmentPlan);
        await dbContext.SaveChangesAsync(ct);
    }

    /// <summary>Tất cả liệu trình của một bệnh nhân (mọi buổi hẹn), kèm số tiền đã thu.</summary>
    public async Task<List<TreatmentPlanDto>> GetByPatientAsync(Guid patientId, CancellationToken ct = default)
    {
        var plans = await dbContext.TreatmentPlans
            .AsNoTracking()
            .Include(tp => tp.Service)
            .Include(tp => tp.Dentist).ThenInclude(d => d.User)
            .Where(tp => tp.PatientId == patientId)
            .OrderBy(tp => tp.CreatedAt)
            .ToListAsync(ct);

        var planIds = plans.Select(p => p.Id).ToList();
        var paidMap = await GetAmountPaidMapAsync(planIds, ct);
        var invoicedIds = await GetInvoicedPlanIdsAsync(planIds, ct);

        return plans
            .Select(p => ToDto(p, paidMap.GetValueOrDefault(p.Id), invoicedIds.Contains(p.Id)))
            .ToList();
    }

    /// <summary>Ghi nhận một bước quy trình đã thực hiện vào nhật ký điều trị của liệu trình.</summary>
    public async Task<TreatmentPlanDto> AddStepProgressAsync(Guid treatmentPlanId, AddStepProgressRequest request, CancellationToken ct = default)
    {
        var treatmentPlan = await dbContext.TreatmentPlans
            .Include(tp => tp.Dentist).ThenInclude(d => d.User)
            .FirstOrDefaultAsync(tp => tp.Id == treatmentPlanId, ct)
            ?? throw new NotFoundException("Không tìm thấy liệu trình điều trị.");

        if (string.IsNullOrWhiteSpace(request.StepName))
            throw new ValidationException("Tên bước điều trị không được để trống.");

        // Chỉ ghi nhận quá trình khi bệnh nhân đang trong buổi khám (bác sĩ đã bấm "Bắt đầu khám")
        var hasActiveVisit = await dbContext.Appointments.AnyAsync(a =>
            a.PatientId == treatmentPlan.PatientId &&
            (a.Status == AppointmentStatus.InProgress || a.Status == AppointmentStatus.PendingPayment || a.Status == AppointmentStatus.Completed), ct);
        if (!hasActiveVisit)
            throw new ValidationException("Chỉ có thể ghi nhận quá trình điều trị khi buổi hẹn đang khám hoặc đã kết thúc điều trị.");

        var entries = ParseStepProgress(treatmentPlan.StepProgressJson);
        entries.Add(new StepProgressEntryDto
        {
            StepNumber = request.StepNumber,
            StepName = request.StepName.Trim(),
            Percent = Math.Clamp(request.Percent, 0, 100),
            Date = request.Date ?? DateOnly.FromDateTime(DateTime.Today),
            DentistName = treatmentPlan.Dentist.FullName,
            Note = NormalizeText(request.Note)
        });

        treatmentPlan.UpdateStepProgress(JsonSerializer.Serialize(entries, JsonOptions));

        // Bắt đầu điều trị khi ghi nhận bước đầu tiên
        if (treatmentPlan.Status == TreatmentPlanStatus.Planned)
            treatmentPlan.SetStatus(TreatmentPlanStatus.InProgress);

        await dbContext.SaveChangesAsync(ct);

        return await LoadDtoAsync(treatmentPlan.Id, ct);
    }

    /// <summary>Sửa một mục đã ghi trong nhật ký điều trị (theo vị trí trong danh sách).</summary>
    public async Task<TreatmentPlanDto> UpdateStepProgressAsync(Guid treatmentPlanId, UpdateStepProgressRequest request, CancellationToken ct = default)
    {
        var treatmentPlan = await dbContext.TreatmentPlans
            .FirstOrDefaultAsync(tp => tp.Id == treatmentPlanId, ct)
            ?? throw new NotFoundException("Không tìm thấy liệu trình điều trị.");

        var hasActiveVisit = await dbContext.Appointments.AnyAsync(a =>
            a.PatientId == treatmentPlan.PatientId &&
            (a.Status == AppointmentStatus.InProgress || a.Status == AppointmentStatus.PendingPayment || a.Status == AppointmentStatus.Completed), ct);
        if (!hasActiveVisit)
            throw new ValidationException("Chỉ có thể sửa quá trình điều trị khi buổi hẹn đang khám hoặc đã kết thúc điều trị.");

        var entries = ParseStepProgress(treatmentPlan.StepProgressJson);
        if (request.EntryIndex < 0 || request.EntryIndex >= entries.Count)
            throw new ValidationException("Không tìm thấy bước điều trị cần sửa.");

        entries[request.EntryIndex].Percent = Math.Clamp(request.Percent, 0, 100);
        entries[request.EntryIndex].Note = NormalizeText(request.Note);
        if (!string.IsNullOrWhiteSpace(request.StepName))
            entries[request.EntryIndex].StepName = request.StepName.Trim();

        treatmentPlan.UpdateStepProgress(JsonSerializer.Serialize(entries, JsonOptions));
        await dbContext.SaveChangesAsync(ct);

        return await LoadDtoAsync(treatmentPlan.Id, ct);
    }

    /// <summary>Sắp xếp lại thứ tự các mục trong nhật ký điều trị. Order là hoán vị các index gốc theo thứ tự mới.</summary>
    public async Task<TreatmentPlanDto> ReorderStepProgressAsync(Guid treatmentPlanId, ReorderStepProgressRequest request, CancellationToken ct = default)
    {
        var treatmentPlan = await dbContext.TreatmentPlans
            .FirstOrDefaultAsync(tp => tp.Id == treatmentPlanId, ct)
            ?? throw new NotFoundException("Không tìm thấy liệu trình điều trị.");

        var hasActiveVisit = await dbContext.Appointments.AnyAsync(a =>
            a.PatientId == treatmentPlan.PatientId &&
            (a.Status == AppointmentStatus.InProgress || a.Status == AppointmentStatus.PendingPayment || a.Status == AppointmentStatus.Completed), ct);
        if (!hasActiveVisit)
            throw new ValidationException("Chỉ có thể đổi thứ tự quá trình điều trị khi buổi hẹn đang khám hoặc đã kết thúc điều trị.");

        var entries = ParseStepProgress(treatmentPlan.StepProgressJson);
        var order = request.Order;
        // Order phải là một hoán vị đầy đủ 0..n-1 (không thiếu, không trùng, không lệch)
        if (order.Count != entries.Count ||
            order.Distinct().Count() != entries.Count ||
            order.Any(i => i < 0 || i >= entries.Count))
            throw new ValidationException("Thứ tự quá trình điều trị không hợp lệ.");

        var reordered = order.Select(i => entries[i]).ToList();
        treatmentPlan.UpdateStepProgress(JsonSerializer.Serialize(reordered, JsonOptions));
        await dbContext.SaveChangesAsync(ct);

        return await LoadDtoAsync(treatmentPlan.Id, ct);
    }

    /// <summary>Xóa một mục đã ghi trong nhật ký điều trị (theo vị trí trong danh sách).</summary>
    public async Task<TreatmentPlanDto> DeleteStepProgressAsync(Guid treatmentPlanId, int entryIndex, CancellationToken ct = default)
    {
        var treatmentPlan = await dbContext.TreatmentPlans
            .FirstOrDefaultAsync(tp => tp.Id == treatmentPlanId, ct)
            ?? throw new NotFoundException("Không tìm thấy liệu trình điều trị.");

        var hasActiveVisit = await dbContext.Appointments.AnyAsync(a =>
            a.PatientId == treatmentPlan.PatientId &&
            (a.Status == AppointmentStatus.InProgress || a.Status == AppointmentStatus.PendingPayment || a.Status == AppointmentStatus.Completed), ct);
        if (!hasActiveVisit)
            throw new ValidationException("Chỉ có thể xóa quá trình điều trị khi buổi hẹn đang khám hoặc đã kết thúc điều trị.");

        var entries = ParseStepProgress(treatmentPlan.StepProgressJson);
        if (entryIndex < 0 || entryIndex >= entries.Count)
            throw new ValidationException("Không tìm thấy bước điều trị cần xóa.");

        entries.RemoveAt(entryIndex);
        treatmentPlan.UpdateStepProgress(entries.Count == 0 ? null : JsonSerializer.Serialize(entries, JsonOptions));
        await dbContext.SaveChangesAsync(ct);

        return await LoadDtoAsync(treatmentPlan.Id, ct);
    }

    public static List<StepProgressEntryDto> ParseStepProgress(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<StepProgressEntryDto>();
        try
        {
            return JsonSerializer.Deserialize<List<StepProgressEntryDto>>(json, JsonOptions) ?? new List<StepProgressEntryDto>();
        }
        catch (JsonException)
        {
            return new List<StepProgressEntryDto>();
        }
    }

    public static TreatmentPlanDto ToDto(TreatmentPlan tp, decimal amountPaid = 0, bool isInvoiced = false) => new()
    {
        Id = tp.Id,
        PatientId = tp.PatientId,
        DentistId = tp.DentistId,
        DentistName = tp.Dentist?.FullName ?? string.Empty,
        AppointmentId = tp.AppointmentId,
        ServiceId = tp.ServiceId,
        ServiceName = tp.Service?.Name ?? string.Empty,
        UnitPrice = tp.UnitPrice,
        Quantity = tp.Quantity,
        Teeth = tp.Teeth,
        Status = tp.Status.ToString(),
        WarrantyUntil = tp.WarrantyUntil,
        Notes = tp.Notes,
        TotalCost = tp.TotalCost,
        AmountPaid = amountPaid,
        IsInvoiced = isInvoiced,
        StepProgress = ParseStepProgress(tp.StepProgressJson),
        CreatedAt = tp.CreatedAt,
        CompletedAt = tp.CompletedAt
    };

    private async Task<TreatmentPlanDto> LoadDtoAsync(Guid planId, CancellationToken ct)
    {
        var plan = await dbContext.TreatmentPlans
            .AsNoTracking()
            .Include(tp => tp.Service)
            .Include(tp => tp.Dentist).ThenInclude(d => d.User)
            .FirstAsync(tp => tp.Id == planId, ct);

        var planIds = new List<Guid> { planId };
        var paid = (await GetAmountPaidMapAsync(planIds, ct)).GetValueOrDefault(planId, 0m);
        var isInvoiced = (await GetInvoicedPlanIdsAsync(planIds, ct)).Contains(planId);

        return ToDto(plan, paid, isInvoiced);
    }

    private async Task<decimal> GetAmountPaidAsync(Guid treatmentPlanId, CancellationToken ct) =>
        (await GetAmountPaidMapAsync(new List<Guid> { treatmentPlanId }, ct)).GetValueOrDefault(treatmentPlanId, 0m);

    /// <summary>
    /// Số tiền ĐÃ THU theo từng liệu trình. Liệu trình được gắn vào hóa đơn theo 2 cách:
    /// dòng hóa đơn gắn liệu trình (mô hình hiện tại) và hóa đơn "đợt thu" gắn ở cấp hóa đơn (mô hình cũ).
    /// </summary>
    private async Task<Dictionary<Guid, decimal>> GetAmountPaidMapAsync(List<Guid> planIds, CancellationToken ct)
    {
        var map = new Dictionary<Guid, decimal>();
        if (planIds.Count == 0) return map;

        var lineRows = await dbContext.InvoiceItems
            .Where(it => it.TreatmentPlanId != null && planIds.Contains(it.TreatmentPlanId.Value)
                         && it.Invoice.Status == PaymentStatus.Paid)
            .Select(it => new
            {
                PlanId = it.TreatmentPlanId!.Value,
                Line = it.Quantity * it.UnitPrice,
                it.AmountCollected,
                it.Invoice.IsSettled
            })
            .ToListAsync(ct);
        foreach (var r in lineRows)
            map[r.PlanId] = map.GetValueOrDefault(r.PlanId, 0m) + (r.IsSettled ? r.Line : r.AmountCollected);

        var headerRows = await dbContext.Invoices
            .Where(i => i.TreatmentPlanId != null && planIds.Contains(i.TreatmentPlanId.Value)
                        && i.Status == PaymentStatus.Paid)
            .GroupBy(i => i.TreatmentPlanId!.Value)
            .Select(g => new { PlanId = g.Key, Sum = g.Sum(x => x.DepositAmount) })
            .ToListAsync(ct);
        foreach (var h in headerRows)
            map[h.PlanId] = map.GetValueOrDefault(h.PlanId, 0m) + h.Sum;

        return map;
    }

    /// <summary>
    /// Các liệu trình đã được xuất hóa đơn (hóa đơn chưa hoàn tiền) — kể cả hóa đơn chưa thanh toán,
    /// vì hóa đơn đã phát hành thì không được sửa danh mục dịch vụ nữa.
    /// </summary>
    private async Task<HashSet<Guid>> GetInvoicedPlanIdsAsync(List<Guid> planIds, CancellationToken ct)
    {
        if (planIds.Count == 0) return new HashSet<Guid>();

        var byLine = await dbContext.InvoiceItems
            .Where(it => it.TreatmentPlanId != null && planIds.Contains(it.TreatmentPlanId.Value)
                         && it.Invoice.Status != PaymentStatus.Refunded)
            .Select(it => it.TreatmentPlanId!.Value)
            .Distinct()
            .ToListAsync(ct);

        var byHeader = await dbContext.Invoices
            .Where(i => i.TreatmentPlanId != null && planIds.Contains(i.TreatmentPlanId.Value)
                        && i.Status != PaymentStatus.Refunded)
            .Select(i => i.TreatmentPlanId!.Value)
            .Distinct()
            .ToListAsync(ct);

        return byLine.Concat(byHeader).ToHashSet();
    }

    private async Task<bool> IsInvoicedAsync(Guid treatmentPlanId, CancellationToken ct) =>
        (await GetInvoicedPlanIdsAsync(new List<Guid> { treatmentPlanId }, ct)).Count > 0;

    private static string? NormalizeText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
