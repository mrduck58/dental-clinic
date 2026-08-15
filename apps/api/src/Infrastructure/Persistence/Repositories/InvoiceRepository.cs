using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Infrastructure.Persistence.Repositories;

public class InvoiceRepository(AppDbContext db) : IInvoiceRepository
{
    private IQueryable<Invoice> QueryWithDetails() =>
        db.Invoices.AsNoTracking()
            .Include(i => i.Items)
            .Include(i => i.Promotion)
            .Include(i => i.Appointment).ThenInclude(a => a.Patient).ThenInclude(p => p.User)
            .Include(i => i.Appointment).ThenInclude(a => a.Dentist).ThenInclude(d => d.Employee).ThenInclude(e => e.User);

    public Task<Invoice?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Invoices.FirstOrDefaultAsync(i => i.Id == id, ct);

    public Task<Invoice?> GetByIdWithAppointmentAsync(Guid id, CancellationToken ct = default) =>
        db.Invoices.Include(i => i.Appointment).FirstOrDefaultAsync(i => i.Id == id, ct);

    public Task<Invoice?> GetByIdWithAppointmentAndPatientAsync(Guid id, CancellationToken ct = default) =>
        db.Invoices.AsNoTracking()
            .Include(i => i.Appointment).ThenInclude(a => a.Patient)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

    public Task<Invoice?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default) =>
        QueryWithDetails().FirstOrDefaultAsync(i => i.Id == id, ct);

    public async Task<IReadOnlyList<Invoice>> GetOutstandingInvoicesAsync(CancellationToken ct = default) =>
        await QueryWithDetails()
            .Where(i => i.Status != PaymentStatus.Refunded && !i.IsSettled
                        && i.ParentInvoiceId == null && i.DepositAmount < i.TotalAmount)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Invoice>> GetPendingInvoicesAsync(CancellationToken ct = default) =>
        await QueryWithDetails()
            .Where(i => i.Status == PaymentStatus.Unpaid)
            .OrderBy(i => i.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Invoice>> GetPendingInvoicesByPatientAsync(Guid patientId, CancellationToken ct = default) =>
        await QueryWithDetails()
            .Where(i => i.Status == PaymentStatus.Unpaid && i.Appointment.PatientId == patientId)
            .OrderBy(i => i.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Invoice>> GetPaidInvoicesByPatientAsync(Guid patientId, CancellationToken ct = default) =>
        await QueryWithDetails()
            .Where(i => i.Status == PaymentStatus.Paid && i.Appointment.PatientId == patientId)
            .OrderByDescending(i => i.PaymentDate)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Invoice>> GetInvoiceHistoryAsync(CancellationToken ct = default) =>
        await QueryWithDetails()
            .Where(i => i.Status == PaymentStatus.Paid)
            .OrderByDescending(i => i.PaymentDate)
            .ToListAsync(ct);

    public Task<bool> HasChildInvoiceAsync(Guid parentInvoiceId, CancellationToken ct = default) =>
        db.Invoices.AnyAsync(c => c.ParentInvoiceId == parentInvoiceId, ct);

    public Task<int> CountAsync(CancellationToken ct = default) =>
        db.Invoices.CountAsync(ct);

    public async Task<IReadOnlyList<Invoice>> GetCollectingRemainingParentsAsync(CancellationToken ct = default) =>
        await db.Invoices.AsNoTracking()
            .Include(i => i.Appointment).ThenInclude(a => a.Patient).ThenInclude(p => p.User)
            .Include(i => i.Appointment).ThenInclude(a => a.Dentist).ThenInclude(d => d.Employee).ThenInclude(e => e.User)
            .Where(i => i.CollectingRemaining && !i.IsSettled && i.TotalAmount > i.DepositAmount
                        && !db.Invoices.Any(c => c.ParentInvoiceId == i.Id))
            .OrderBy(i => i.CreatedAt)
            .ToListAsync(ct);

    public void Add(Invoice invoice) => db.Invoices.Add(invoice);

    public Task<Guid?> GetPatientUserIdByAppointmentIdAsync(Guid appointmentId, CancellationToken ct = default) =>
        db.Appointments
            .Where(a => a.Id == appointmentId)
            .Select(a => (Guid?)a.Patient.UserId)
            .FirstOrDefaultAsync(ct);

    public Task<Appointment?> GetAppointmentWithInvoicesAsync(Guid appointmentId, CancellationToken ct = default) =>
        db.Appointments.Include(a => a.Invoices).FirstOrDefaultAsync(a => a.Id == appointmentId, ct);

    public async Task<IReadOnlyList<Appointment>> GetPendingPaymentAppointmentsWithDetailsAsync(CancellationToken ct = default) =>
        await db.Appointments.AsNoTracking()
            .Include(a => a.Patient).ThenInclude(p => p.User)
            .Include(a => a.Dentist).ThenInclude(d => d.Employee).ThenInclude(e => e.User)
            .Include(a => a.Diagnoses)
            .Where(a => a.Status == AppointmentStatus.PendingPayment)
            .OrderBy(a => a.AppointmentDate)
            .ToListAsync(ct);

    public async Task<Dictionary<Guid, Guid?>> GetFollowUpParentMapAsync(IReadOnlyList<Guid> patientIds, CancellationToken ct = default)
    {
        if (patientIds.Count == 0) return new Dictionary<Guid, Guid?>();
        return (await db.Appointments.AsNoTracking()
                .Where(a => patientIds.Contains(a.PatientId))
                .Select(a => new { a.Id, a.FollowUpFromAppointmentId })
                .ToListAsync(ct))
            .ToDictionary(a => a.Id, a => a.FollowUpFromAppointmentId);
    }

    public async Task<HashSet<Guid>> GetFollowUpChainAsync(Guid appointmentId, CancellationToken ct = default)
    {
        var chain = new HashSet<Guid>();
        Guid? cursor = appointmentId;
        while (cursor is Guid c && chain.Add(c))
            cursor = await db.Appointments
                .Where(a => a.Id == c)
                .Select(a => a.FollowUpFromAppointmentId)
                .FirstOrDefaultAsync(ct);
        return chain;
    }

    public Task<TreatmentPlan?> GetTreatmentPlanWithServiceAsync(Guid treatmentPlanId, CancellationToken ct = default) =>
        db.TreatmentPlans.Include(tp => tp.Service).FirstOrDefaultAsync(tp => tp.Id == treatmentPlanId, ct);

    public async Task<IReadOnlyList<TreatmentPlan>> GetActiveTreatmentPlansByPatientIdsAsync(
        IReadOnlyList<Guid> patientIds, CancellationToken ct = default)
    {
        if (patientIds.Count == 0) return [];
        return await db.TreatmentPlans.AsNoTracking()
            .Include(tp => tp.Service)
            .Where(tp => patientIds.Contains(tp.PatientId) && tp.Status != TreatmentPlanStatus.Cancelled)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<TreatmentPlanBillingInfo>> GetTreatmentPlanBillingInfoAsync(
        IReadOnlyList<Guid> treatmentPlanIds, CancellationToken ct = default) =>
        await db.TreatmentPlans
            .Where(tp => treatmentPlanIds.Contains(tp.Id))
            .Select(tp => new TreatmentPlanBillingInfo(tp.Id, tp.UnitPrice, tp.Quantity))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<TreatmentPlan>> GetInProgressTreatmentPlansWithDetailsAsync(CancellationToken ct = default) =>
        await db.TreatmentPlans.AsNoTracking()
            .Include(tp => tp.Patient).ThenInclude(p => p.User)
            .Include(tp => tp.Dentist).ThenInclude(d => d.Employee).ThenInclude(e => e.User)
            .Include(tp => tp.Service)
            .Where(tp => tp.Status == TreatmentPlanStatus.InProgress)
            .OrderByDescending(tp => tp.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<TreatmentPlanBillingInfo>> GetTreatmentPlanBillingInfoByAppointmentIdsAsync(
        IReadOnlyList<Guid> appointmentIds, CancellationToken ct = default) =>
        await db.TreatmentPlans
            .Where(tp => tp.AppointmentId != null && appointmentIds.Contains(tp.AppointmentId.Value)
                         && tp.Status != TreatmentPlanStatus.Cancelled)
            .Select(tp => new TreatmentPlanBillingInfo(tp.Id, tp.UnitPrice, tp.Quantity))
            .ToListAsync(ct);
}
