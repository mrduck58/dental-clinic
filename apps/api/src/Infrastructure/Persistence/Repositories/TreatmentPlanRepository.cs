namespace DentalClinic.API.Infrastructure.Persistence.Repositories;

using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

public class TreatmentPlanRepository(AppDbContext db) : ITreatmentPlanRepository
{
    public async Task<TreatmentPlan?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.TreatmentPlans
            .Include(tp => tp.Items).ThenInclude(i => i.Service)
            .Include(tp => tp.Items).ThenInclude(i => i.Sessions)
            .FirstOrDefaultAsync(tp => tp.Id == id, ct);

    public async Task<TreatmentPlan?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default)
        => await db.TreatmentPlans
            .Include(tp => tp.Dentist).ThenInclude(d => d.Employee).ThenInclude(e => e.User)
            .Include(tp => tp.Items).ThenInclude(i => i.Service)
            .Include(tp => tp.Items).ThenInclude(i => i.ServiceOption)
            .Include(tp => tp.Items).ThenInclude(i => i.Sessions).ThenInclude(s => s.Dentist).ThenInclude(d => d!.Employee).ThenInclude(e => e.User)
            .FirstOrDefaultAsync(tp => tp.Id == id, ct);

    public async Task<TreatmentPlan?> GetByIdWithDentistAsync(Guid id, CancellationToken ct = default)
        => await db.TreatmentPlans
            .Include(tp => tp.Dentist).ThenInclude(d => d.Employee).ThenInclude(e => e.User)
            .Include(tp => tp.Items).ThenInclude(i => i.Service)
            .Include(tp => tp.Items).ThenInclude(i => i.Sessions)
            .FirstOrDefaultAsync(tp => tp.Id == id, ct);

    public async Task<TreatmentPlanItem?> GetItemByIdAsync(Guid itemId, CancellationToken ct = default)
        => await db.TreatmentPlanItems
            .Include(i => i.TreatmentPlan)
            .Include(i => i.Service)
            .Include(i => i.Sessions)
            .FirstOrDefaultAsync(i => i.Id == itemId, ct);

    public async Task<TreatmentPlanItem?> GetItemWithDetailsAsync(Guid itemId, CancellationToken ct = default)
        => await db.TreatmentPlanItems
            .AsNoTracking()
            .Include(i => i.TreatmentPlan).ThenInclude(tp => tp.Dentist).ThenInclude(d => d.Employee).ThenInclude(e => e.User)
            .Include(i => i.Service)
            .Include(i => i.ServiceOption)
            .Include(i => i.Sessions).ThenInclude(s => s.Dentist).ThenInclude(d => d!.Employee).ThenInclude(e => e.User)
            .FirstOrDefaultAsync(i => i.Id == itemId, ct);

    public async Task<TreatmentSession?> GetSessionByIdAsync(Guid sessionId, CancellationToken ct = default)
        => await db.TreatmentSessions
            .Include(s => s.TreatmentPlanItem).ThenInclude(i => i.TreatmentPlan)
            .Include(s => s.TreatmentPlanItem).ThenInclude(i => i.Service)
            .Include(s => s.Dentist).ThenInclude(d => d!.Employee).ThenInclude(e => e.User)
            .FirstOrDefaultAsync(s => s.Id == sessionId, ct);

    public async Task<IReadOnlyList<TreatmentPlan>> GetByPatientIdAsync(Guid patientId, CancellationToken ct = default)
        => await db.TreatmentPlans
            .AsNoTracking()
            .Include(tp => tp.Dentist).ThenInclude(d => d.Employee).ThenInclude(e => e.User)
            .Include(tp => tp.Items).ThenInclude(i => i.Service)
            .Include(tp => tp.Items).ThenInclude(i => i.ServiceOption)
            .Include(tp => tp.Items).ThenInclude(i => i.Sessions).ThenInclude(s => s.Dentist).ThenInclude(d => d!.Employee).ThenInclude(e => e.User)
            .Where(tp => tp.PatientId == patientId)
            .OrderBy(tp => tp.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<TreatmentPlan>> GetAllWithServiceAsync(CancellationToken ct = default)
        => await db.TreatmentPlans
            .AsNoTracking()
            .Include(tp => tp.Items).ThenInclude(i => i.Service)
            .Where(tp => tp.Status != TreatmentPlanStatus.Cancelled)
            .ToListAsync(ct);

    public async Task AddAsync(TreatmentPlan treatmentPlan, CancellationToken ct = default)
    {
        await db.TreatmentPlans.AddAsync(treatmentPlan, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(TreatmentPlan treatmentPlan, CancellationToken ct = default)
    {
        var local = db.TreatmentPlans.Local.FirstOrDefault(e => e.Id == treatmentPlan.Id);
        if (local != null && !ReferenceEquals(local, treatmentPlan))
        {
            db.Entry(local).CurrentValues.SetValues(treatmentPlan);
        }
        else if (local == null)
        {
            db.TreatmentPlans.Attach(treatmentPlan);
            db.Entry(treatmentPlan).State = EntityState.Modified;
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(TreatmentPlan treatmentPlan, CancellationToken ct = default)
    {
        var local = db.TreatmentPlans.Local.FirstOrDefault(e => e.Id == treatmentPlan.Id);
        if (local != null)
            db.TreatmentPlans.Remove(local);
        else
        {
            db.TreatmentPlans.Attach(treatmentPlan);
            db.TreatmentPlans.Remove(treatmentPlan);
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task AddItemAsync(TreatmentPlanItem item, CancellationToken ct = default)
    {
        var local = db.TreatmentPlanItems.Local.FirstOrDefault(e => e.Id == item.Id);
        if (local == null)
            await db.TreatmentPlanItems.AddAsync(item, ct);
        else
            db.Entry(local).State = EntityState.Added;
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateItemAsync(TreatmentPlanItem item, CancellationToken ct = default)
    {
        var local = db.TreatmentPlanItems.Local.FirstOrDefault(e => e.Id == item.Id);
        if (local != null && !ReferenceEquals(local, item))
        {
            db.Entry(local).CurrentValues.SetValues(item);
        }
        else if (local == null)
        {
            db.TreatmentPlanItems.Attach(item);
            db.Entry(item).State = EntityState.Modified;
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteItemAsync(TreatmentPlanItem item, CancellationToken ct = default)
    {
        var local = db.TreatmentPlanItems.Local.FirstOrDefault(e => e.Id == item.Id);
        if (local != null)
            db.TreatmentPlanItems.Remove(local);
        else
        {
            db.TreatmentPlanItems.Attach(item);
            db.TreatmentPlanItems.Remove(item);
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task AddSessionAsync(TreatmentSession session, CancellationToken ct = default)
    {
        var local = db.TreatmentSessions.Local.FirstOrDefault(e => e.Id == session.Id);
        if (local == null)
            await db.TreatmentSessions.AddAsync(session, ct);
        else
            db.Entry(local).State = EntityState.Added;
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateSessionAsync(TreatmentSession session, CancellationToken ct = default)
    {
        var local = db.TreatmentSessions.Local.FirstOrDefault(e => e.Id == session.Id);
        if (local != null && !ReferenceEquals(local, session))
        {
            db.Entry(local).CurrentValues.SetValues(session);
        }
        else if (local == null)
        {
            db.TreatmentSessions.Attach(session);
            db.Entry(session).State = EntityState.Modified;
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteSessionAsync(TreatmentSession session, CancellationToken ct = default)
    {
        var local = db.TreatmentSessions.Local.FirstOrDefault(e => e.Id == session.Id);
        if (local != null)
            db.TreatmentSessions.Remove(local);
        else
        {
            db.TreatmentSessions.Attach(session);
            db.TreatmentSessions.Remove(session);
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task<Dictionary<Guid, decimal>> GetPlanPaidMapAsync(List<Guid> planIds, CancellationToken ct = default)
    {
        var map = new Dictionary<Guid, decimal>();
        if (planIds.Count == 0) return map;

        var lineRows = await db.InvoiceItems
            .Where(it => ((it.TreatmentPlanId != null && planIds.Contains(it.TreatmentPlanId.Value)) ||
                          (it.TreatmentPlanItemId != null && db.TreatmentPlanItems.Where(tpi => planIds.Contains(tpi.TreatmentPlanId)).Select(tpi => tpi.Id).Contains(it.TreatmentPlanItemId.Value)))
                         && it.Invoice.Status == PaymentStatus.Paid)
            .Select(it => new
            {
                PlanId = it.TreatmentPlanId ?? (it.TreatmentPlanItem != null ? it.TreatmentPlanItem.TreatmentPlanId : Guid.Empty),
                Line = it.Quantity * it.UnitPrice,
                it.AmountCollected,
                it.Invoice.IsSettled,
                it.Invoice.Subtotal,
                it.Invoice.TotalAmount
            })
            .ToListAsync(ct);

        foreach (var r in lineRows)
        {
            if (r.PlanId == Guid.Empty) continue;
            var credit = r.IsSettled
                ? (r.Subtotal > 0 ? r.Line * r.TotalAmount / r.Subtotal : r.Line)
                : r.AmountCollected;
            map[r.PlanId] = map.GetValueOrDefault(r.PlanId, 0m) + credit;
        }

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
            .Where(it => ((it.TreatmentPlanId != null && planIds.Contains(it.TreatmentPlanId.Value)) ||
                          (it.TreatmentPlanItemId != null && db.TreatmentPlanItems.Where(tpi => planIds.Contains(tpi.TreatmentPlanId)).Select(tpi => tpi.Id).Contains(it.TreatmentPlanItemId.Value)))
                         && it.Invoice.Status != PaymentStatus.Refunded)
            .Select(it => new
            {
                PlanId = it.TreatmentPlanId ?? (it.TreatmentPlanItem != null ? it.TreatmentPlanItem.TreatmentPlanId : Guid.Empty),
                Total = it.Quantity * it.UnitPrice
            })
            .ToListAsync(ct);

        foreach (var r in byLine)
        {
            if (r.PlanId == Guid.Empty) continue;
            map[r.PlanId] = map.GetValueOrDefault(r.PlanId, 0m) + r.Total;
        }

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
    {
        var items = await db.TreatmentPlanItems
            .AsNoTracking()
            .Include(i => i.TreatmentPlan)
            .Include(i => i.Service)
            .Where(i => (i.Status == TreatmentPlanItemStatus.InProgress || i.TreatmentPlan.Status == TreatmentPlanStatus.InProgress)
                        && i.TreatmentPlan.AppointmentId != null && patientIds.Contains(i.TreatmentPlan.PatientId))
            .Select(i => new ActiveTreatmentPlanSummary(i.TreatmentPlan.AppointmentId!.Value, i.ServiceId, i.Service.Name))
            .ToListAsync(ct);

        return items;
    }
}
