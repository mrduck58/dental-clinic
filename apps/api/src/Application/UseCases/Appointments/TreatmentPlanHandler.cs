using System.Text.Json;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
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
    public List<StepProgressEntryDto> StepProgress { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}

public class TreatmentPlanHandler(AppDbContext dbContext)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<TreatmentPlanDto> CreateAsync(CreateTreatmentPlanRequest request, CancellationToken ct = default)
    {
        var appointment = await dbContext.Appointments
            .FirstOrDefaultAsync(a => a.Id == request.AppointmentId, ct)
            ?? throw new NotFoundException("Không tìm thấy lịch hẹn.");

        if (appointment.Status != AppointmentStatus.InProgress)
            throw new ValidationException("Chỉ có thể thêm liệu trình khi cuộc hẹn đang trong trạng thái đang khám.");

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

        var hasInvoices = await dbContext.Invoices.AnyAsync(i => i.TreatmentPlanId == treatmentPlanId, ct);
        if (hasInvoices)
            throw new ValidationException("Không thể xóa liệu trình đã có hóa đơn thu tiền.");

        dbContext.TreatmentPlans.Remove(treatmentPlan);
        await dbContext.SaveChangesAsync(ct);
    }

    /// <summary>Tất cả liệu trình của một bệnh nhân (mọi buổi hẹn), kèm số tiền đã thu.</summary>
    public async Task<List<TreatmentPlanDto>> GetByPatientAsync(Guid patientId, CancellationToken ct = default)
    {
        var plans = await dbContext.TreatmentPlans
            .AsNoTracking()
            .Include(tp => tp.Service)
            .Include(tp => tp.Dentist)
            .Where(tp => tp.PatientId == patientId)
            .OrderBy(tp => tp.CreatedAt)
            .ToListAsync(ct);

        var planIds = plans.Select(p => p.Id).ToList();
        var paidMap = await dbContext.Invoices
            .Where(i => i.TreatmentPlanId != null && planIds.Contains(i.TreatmentPlanId.Value) && i.Status == PaymentStatus.Paid)
            .GroupBy(i => i.TreatmentPlanId!.Value)
            .Select(g => new { PlanId = g.Key, Paid = g.Sum(i => i.TotalAmount) })
            .ToDictionaryAsync(x => x.PlanId, x => x.Paid, ct);

        return plans.Select(p => ToDto(p, paidMap.GetValueOrDefault(p.Id))).ToList();
    }

    /// <summary>Ghi nhận một bước quy trình đã thực hiện vào nhật ký điều trị của liệu trình.</summary>
    public async Task<TreatmentPlanDto> AddStepProgressAsync(Guid treatmentPlanId, AddStepProgressRequest request, CancellationToken ct = default)
    {
        var treatmentPlan = await dbContext.TreatmentPlans
            .Include(tp => tp.Dentist)
            .FirstOrDefaultAsync(tp => tp.Id == treatmentPlanId, ct)
            ?? throw new NotFoundException("Không tìm thấy liệu trình điều trị.");

        if (string.IsNullOrWhiteSpace(request.StepName))
            throw new ValidationException("Tên bước điều trị không được để trống.");

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

    public static TreatmentPlanDto ToDto(TreatmentPlan tp, decimal amountPaid = 0) => new()
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
        StepProgress = ParseStepProgress(tp.StepProgressJson),
        CreatedAt = tp.CreatedAt,
        CompletedAt = tp.CompletedAt
    };

    private async Task<TreatmentPlanDto> LoadDtoAsync(Guid planId, CancellationToken ct)
    {
        var plan = await dbContext.TreatmentPlans
            .AsNoTracking()
            .Include(tp => tp.Service)
            .Include(tp => tp.Dentist)
            .FirstAsync(tp => tp.Id == planId, ct);

        return ToDto(plan, await GetAmountPaidAsync(planId, ct));
    }

    private async Task<decimal> GetAmountPaidAsync(Guid treatmentPlanId, CancellationToken ct) =>
        await dbContext.Invoices
            .Where(i => i.TreatmentPlanId == treatmentPlanId && i.Status == PaymentStatus.Paid)
            .SumAsync(i => (decimal?)i.TotalAmount, ct) ?? 0m;

    private static string? NormalizeText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
