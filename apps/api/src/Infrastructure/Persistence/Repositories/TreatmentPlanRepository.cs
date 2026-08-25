using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Infrastructure.Persistence.Repositories;

public class TreatmentPlanRepository(AppDbContext db) : ITreatmentPlanRepository
{
    public async Task<TreatmentPlan?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.TreatmentPlans.FirstOrDefaultAsync(tp => tp.Id == id, ct);

    public async Task<TreatmentPlan?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default)
        => await db.TreatmentPlans
            .AsNoTracking()
            .Include(tp => tp.Service)
            .Include(tp => tp.Dentist).ThenInclude(d => d.Employee).ThenInclude(e => e.User)
            .FirstOrDefaultAsync(tp => tp.Id == id, ct);

    public async Task<TreatmentPlan?> GetByIdWithDentistAsync(Guid id, CancellationToken ct = default)
        => await db.TreatmentPlans
            .Include(tp => tp.Dentist).ThenInclude(d => d.Employee).ThenInclude(e => e.User)
            .FirstOrDefaultAsync(tp => tp.Id == id, ct);

    public async Task<IReadOnlyList<TreatmentPlan>> GetByPatientIdAsync(Guid patientId, CancellationToken ct = default)
        => await db.TreatmentPlans
            .AsNoTracking()
            .Include(tp => tp.Service)
            .Include(tp => tp.Dentist).ThenInclude(d => d.Employee).ThenInclude(e => e.User)
            .Where(tp => tp.PatientId == patientId)
            .OrderBy(tp => tp.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<TreatmentPlan>> GetAllWithServiceAsync(CancellationToken ct = default)
        => await db.TreatmentPlans
            .AsNoTracking()
            .Include(tp => tp.Service)
            .Where(tp => tp.Status != TreatmentPlanStatus.Cancelled)
            .ToListAsync(ct);

    public async Task AddAsync(TreatmentPlan treatmentPlan, CancellationToken ct = default)
    {
        await db.TreatmentPlans.AddAsync(treatmentPlan, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(TreatmentPlan treatmentPlan, CancellationToken ct = default)
    {
        db.TreatmentPlans.Update(treatmentPlan);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(TreatmentPlan treatmentPlan, CancellationToken ct = default)
    {
        db.TreatmentPlans.Remove(treatmentPlan);
        await db.SaveChangesAsync(ct);
    }

    public async Task<Dictionary<Guid, decimal>> GetPlanPaidMapAsync(List<Guid> planIds, CancellationToken ct = default)
    {
        var map = new Dictionary<Guid, decimal>();
        if (planIds.Count == 0) return map;

        // Mô hình mới: dòng hóa đơn gắn TreatmentPlanId, credit theo số thu của từng dòng.
        var lineRows = await db.InvoiceItems
            .Where(it => it.TreatmentPlanId != null && planIds.Contains(it.TreatmentPlanId.Value)
                         && it.Invoice.Status == PaymentStatus.Paid)
            .Select(it => new
            {
                PlanId = it.TreatmentPlanId!.Value,
                Line = it.Quantity * it.UnitPrice,
                it.AmountCollected,
                it.Invoice.IsSettled,
                it.Invoice.Subtotal,
                it.Invoice.TotalAmount
            })
            .ToListAsync(ct);
        foreach (var r in lineRows)
        {
            // Hóa đơn đã tất toán (đặt cọc + thu phần còn lại) credit trọn dòng — NHƯNG nếu hóa đơn có
            // khuyến mãi, tiền thực thu chỉ là TotalAmount (đã giảm giá), không phải tổng thành tiền
            // thô (Subtotal) của các dòng. Quy đổi theo đúng tỉ lệ giảm giá của hóa đơn để không credit
            // vượt quá số tiền bệnh nhân thực sự đã trả.
            var credit = r.IsSettled
                ? (r.Subtotal > 0 ? r.Line * r.TotalAmount / r.Subtotal : r.Line)
                : r.AmountCollected;
            map[r.PlanId] = map.GetValueOrDefault(r.PlanId, 0m) + credit;
        }

        // Mô hình cũ: hóa đơn "đợt thu" gắn liệu trình ở cấp hóa đơn.
        var headerRows = await db.Invoices
            .Where(i => i.TreatmentPlanId != null && planIds.Contains(i.TreatmentPlanId.Value)
                        && i.Status == PaymentStatus.Paid)
            .GroupBy(i => i.TreatmentPlanId!.Value)
            .Select(g => new { PlanId = g.Key, Sum = g.Sum(x => x.DepositAmount) })
            .ToListAsync(ct);
        foreach (var h in headerRows)
            map[h.PlanId] = map.GetValueOrDefault(h.PlanId, 0m) + h.Sum;

        return map;
    }

    public async Task<Dictionary<Guid, decimal>> GetPlanBilledMapAsync(List<Guid> planIds, CancellationToken ct = default)
    {
        var map = new Dictionary<Guid, decimal>();
        if (planIds.Count == 0) return map;

        var byLine = await db.InvoiceItems
            .Where(it => it.TreatmentPlanId != null && planIds.Contains(it.TreatmentPlanId.Value)
                         && it.Invoice.Status != PaymentStatus.Refunded)
            .GroupBy(it => it.TreatmentPlanId!.Value)
            .Select(g => new { PlanId = g.Key, Sum = g.Sum(it => it.Quantity * it.UnitPrice) })
            .ToListAsync(ct);
        foreach (var r in byLine)
            map[r.PlanId] = map.GetValueOrDefault(r.PlanId, 0m) + r.Sum;

        var byHeader = await db.Invoices
            .Where(i => i.TreatmentPlanId != null && planIds.Contains(i.TreatmentPlanId.Value)
                        && i.Status != PaymentStatus.Refunded)
            .GroupBy(i => i.TreatmentPlanId!.Value)
            .Select(g => new { PlanId = g.Key, Sum = g.Sum(x => x.TotalAmount) })
            .ToListAsync(ct);
        foreach (var h in byHeader)
            map[h.PlanId] = map.GetValueOrDefault(h.PlanId, 0m) + h.Sum;

        return map;
    }

    public async Task<IReadOnlyList<ActiveTreatmentPlanSummary>> GetActiveByPatientIdsAsync(List<Guid> patientIds, CancellationToken ct = default)
        => await db.TreatmentPlans
            .AsNoTracking()
            .Include(tp => tp.Service)
            .Where(tp => tp.Status == TreatmentPlanStatus.InProgress && tp.AppointmentId != null && patientIds.Contains(tp.PatientId))
            .Select(tp => new ActiveTreatmentPlanSummary(tp.AppointmentId!.Value, tp.Service.Name))
            .ToListAsync(ct);
}
