using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Infrastructure.Persistence.Repositories;

public class AppointmentRepository(AppDbContext dbContext) : IAppointmentRepository
{
    public async Task<Appointment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Appointments
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Appointment>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Appointments
            .Where(a => a.PatientId == patientId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Appointment>> GetByDentistIdAsync(Guid dentistId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Appointments
            .Where(a => a.DentistId == dentistId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Appointment>> GetByDateAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        // Client gửi giờ VN local, .toUtc() → VD: 07:30 VN = 00:30 UTC.
        // Lọc theo cửa sổ VN midnight, convert sang UTC (offset=0) để Npgsql chấp nhận.
        var vnOffset = TimeSpan.FromHours(7);
        var startUtc = new DateTimeOffset(date.Year, date.Month, date.Day, 0, 0, 0, vnOffset).ToUniversalTime();
        var endUtc   = startUtc.AddDays(1);
        return await dbContext.Appointments
            .Include(a => a.Service)
            .Where(a => a.AppointmentDate >= startUtc && a.AppointmentDate < endUtc
                     && a.Status != DentalClinic.API.Domain.Enums.AppointmentStatus.Cancelled)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Appointment>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Appointments
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Appointment appointment, CancellationToken cancellationToken = default)
    {
        await dbContext.Appointments.AddAsync(appointment, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Appointment appointment, CancellationToken cancellationToken = default)
    {
        dbContext.Appointments.Update(appointment);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid?> GetDentistUserIdAsync(Guid dentistId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<DentistProfile>()
            .Where(d => d.Id == dentistId)
            .Select(d => (Guid?)d.Employee.UserId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Appointment?> GetInProgressByDentistAsync(
        Guid dentistId, Guid excludeAppointmentId,
        DateTimeOffset utcStart, DateTimeOffset utcEnd,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Appointments
            .Include(a => a.Patient)
            .FirstOrDefaultAsync(a =>
                a.DentistId == dentistId &&
                a.Id != excludeAppointmentId &&
                a.Status == DentalClinic.API.Domain.Enums.AppointmentStatus.InProgress &&
                a.AppointmentDate >= utcStart &&
                a.AppointmentDate < utcEnd, cancellationToken);
    }

    public async Task<bool> HasActiveVisitAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Appointments.AnyAsync(a =>
            a.PatientId == patientId &&
            (a.Status == DentalClinic.API.Domain.Enums.AppointmentStatus.InProgress ||
             a.Status == DentalClinic.API.Domain.Enums.AppointmentStatus.PendingPayment ||
             a.Status == DentalClinic.API.Domain.Enums.AppointmentStatus.Completed), cancellationToken);
    }

    public async Task<bool> HasCompletedVisitAsync(Guid dentistId, Guid patientId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Appointments.AnyAsync(a =>
            a.DentistId == dentistId && a.PatientId == patientId &&
            (a.Status == DentalClinic.API.Domain.Enums.AppointmentStatus.Completed ||
             a.Status == DentalClinic.API.Domain.Enums.AppointmentStatus.PendingPayment), cancellationToken);
    }

    public async Task<int> CountDistinctPatientsWithCompletedVisitAsync(Guid dentistId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Appointments
            .Where(a => a.DentistId == dentistId &&
                        (a.Status == DentalClinic.API.Domain.Enums.AppointmentStatus.Completed ||
                         a.Status == DentalClinic.API.Domain.Enums.AppointmentStatus.PendingPayment))
            .Select(a => a.PatientId)
            .Distinct()
            .CountAsync(cancellationToken);
    }

    public async Task<int> CountCompletedVisitsAsync(Guid dentistId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Appointments
            .Where(a => a.DentistId == dentistId &&
                        (a.Status == DentalClinic.API.Domain.Enums.AppointmentStatus.Completed ||
                         a.Status == DentalClinic.API.Domain.Enums.AppointmentStatus.PendingPayment))
            .CountAsync(cancellationToken);
    }

    public async Task<int> CountCompletedVisitsForPatientAsync(Guid dentistId, Guid patientId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Appointments
            .Where(a => a.DentistId == dentistId &&
                        (a.PatientId == patientId || a.Patient.PrimaryPatientId == patientId) &&
                        (a.Status == DentalClinic.API.Domain.Enums.AppointmentStatus.Completed ||
                         a.Status == DentalClinic.API.Domain.Enums.AppointmentStatus.PendingPayment))
            .CountAsync(cancellationToken);
    }

    public async Task<int> CountOverallCompletedVisitsAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Appointments
            .Where(a => (a.PatientId == patientId || a.Patient.PrimaryPatientId == patientId) &&
                        (a.Status == DentalClinic.API.Domain.Enums.AppointmentStatus.Completed ||
                         a.Status == DentalClinic.API.Domain.Enums.AppointmentStatus.PendingPayment))
            .CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetTopServiceNamesByDentistAsync(
        Guid dentistId, int take, CancellationToken cancellationToken = default)
    {
        return await dbContext.Appointments
            .Where(a => a.DentistId == dentistId &&
                        (a.Status == DentalClinic.API.Domain.Enums.AppointmentStatus.Completed ||
                         a.Status == DentalClinic.API.Domain.Enums.AppointmentStatus.PendingPayment) &&
                        a.Service != null && a.Service.Name != null)
            .GroupBy(a => a.Service!.Name)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key!)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsSlotBookedAsync(Guid dentistId, DateTimeOffset appointmentDate, CancellationToken cancellationToken = default)
    {
        var utcDate = appointmentDate.ToUniversalTime();
        return await dbContext.Appointments.AnyAsync(a =>
            a.DentistId == dentistId &&
            a.AppointmentDate == utcDate &&
            a.Status != AppointmentStatus.Cancelled, cancellationToken);
    }

    public async Task<IReadOnlyList<Appointment>> GetStaffAppointmentsAsync(
        DateOnly? date, AppointmentStatus? status, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Appointments
            .Include(a => a.Patient).ThenInclude(p => p.User)
            .Include(a => a.Patient).ThenInclude(p => p.PrimaryPatient).ThenInclude(pp => pp!.User)
            .Include(a => a.Dentist).ThenInclude(d => d.Employee).ThenInclude(e => e.User)
            .Include(a => a.Service)
            .AsQueryable();

        if (date.HasValue)
        {
            // Cửa sổ ngày tính theo giờ VN rồi quy về UTC — giống GetByDateAsync. Dùng mốc nửa đêm UTC
            // sẽ lệch 7 tiếng: lịch hẹn 00:00–07:00 sáng bị tính sang ngày hôm trước.
            var vnOffset = TimeSpan.FromHours(7);
            var start = new DateTimeOffset(date.Value.Year, date.Value.Month, date.Value.Day, 0, 0, 0, vnOffset).ToUniversalTime();
            var end = start.AddDays(1);
            query = query.Where(a => a.AppointmentDate >= start && a.AppointmentDate < end);
        }

        if (status.HasValue)
        {
            query = query.Where(a => a.Status == status.Value);
        }

        return await query.OrderByDescending(a => a.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<Appointment> Items, int TotalCount)> GetAppointmentsPagedAsync(
        DateOnly? startDate, DateOnly? endDate, string? statusCsv, string? search,
        int page, int pageSize, string? sortDir = null, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Appointments
            .Include(a => a.Patient).ThenInclude(p => p.User)
            .Include(a => a.Patient).ThenInclude(p => p.PrimaryPatient).ThenInclude(pp => pp!.User)
            .Include(a => a.Dentist).ThenInclude(d => d.Employee).ThenInclude(e => e.User)
            .Include(a => a.Service)
            .AsQueryable();

        if (startDate.HasValue)
        {
            var start = new DateTimeOffset(startDate.Value.Year, startDate.Value.Month, startDate.Value.Day, 0, 0, 0, TimeSpan.Zero);
            query = query.Where(a => a.AppointmentDate >= start);
        }

        if (endDate.HasValue)
        {
            var endExclusive = new DateTimeOffset(endDate.Value.Year, endDate.Value.Month, endDate.Value.Day, 0, 0, 0, TimeSpan.Zero).AddDays(1);
            query = query.Where(a => a.AppointmentDate < endExclusive);
        }

        if (!string.IsNullOrWhiteSpace(statusCsv))
        {
            var statuses = statusCsv
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => Enum.TryParse<AppointmentStatus>(s, ignoreCase: true, out var parsed) ? (AppointmentStatus?)parsed : null)
                .Where(s => s.HasValue)
                .Select(s => s!.Value)
                .ToArray();
            if (statuses.Length > 0)
                query = query.Where(a => statuses.Contains(a.Status));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(a =>
                a.Patient.FullName.ToLower().Contains(term) ||
                (a.Patient.User != null && a.Patient.User.PhoneNumber != null && a.Patient.User.PhoneNumber.Contains(term)) ||
                a.Dentist.FullName.ToLower().Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);

        var ordered = string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase)
            ? query.OrderBy(a => a.AppointmentDate)
            : query.OrderByDescending(a => a.AppointmentDate);

        var items = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<IReadOnlyList<Appointment>> GetByPatientIdWithDetailsAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Appointments
            .Include(a => a.Dentist).ThenInclude(d => d.Employee).ThenInclude(e => e.User)
            .Include(a => a.Service)
            .Include(a => a.Invoices)
            .Where(a => a.PatientId == patientId)
            .OrderByDescending(a => a.AppointmentDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Appointment>> GetMyAppointmentsAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Appointments
            .Include(a => a.Dentist).ThenInclude(d => d.Employee).ThenInclude(e => e.User)
            .Include(a => a.Patient).ThenInclude(p => p.User)
            .Include(a => a.Service)
            .Where(a => a.PatientId == patientId || a.Patient.PrimaryPatientId == patientId)
            .OrderByDescending(a => a.AppointmentDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Appointment>> GetActiveInRangeAsync(DateTimeOffset utcStart, DateTimeOffset utcEnd, CancellationToken cancellationToken = default)
    {
        return await dbContext.Appointments
            .Include(a => a.Patient).ThenInclude(p => p.User)
            .Include(a => a.Service)
            .Where(a => a.AppointmentDate >= utcStart && a.AppointmentDate < utcEnd &&
                        a.Status != AppointmentStatus.Cancelled)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Appointment>> GetActiveByDentistIdAsync(Guid dentistId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Appointments
            .Include(a => a.Service)
            .Where(a => a.DentistId == dentistId && a.Status != AppointmentStatus.Cancelled)
            .ToListAsync(cancellationToken);
    }

    public async Task<Appointment?> GetExaminationDetailAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Appointments
            .Include(a => a.Patient).ThenInclude(p => p.User)
            .Include(a => a.Dentist).ThenInclude(d => d.Employee).ThenInclude(e => e.User)
            .Include(a => a.Service)
            .Include(a => a.Diagnoses)
            .Include(a => a.TreatmentPlans).ThenInclude(tp => tp.Service)
            .Include(a => a.TreatmentPlans).ThenInclude(tp => tp.Dentist)
            .Include(a => a.Prescriptions).ThenInclude(p => p.Items)
            .FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken);
    }

    public async Task<IReadOnlyList<Appointment>> GetCompletedHistoryByPatientAsync(Guid patientId, int take, CancellationToken cancellationToken = default)
    {
        return await dbContext.Appointments
            .Include(a => a.Dentist).ThenInclude(d => d.Employee).ThenInclude(e => e.User)
            .Include(a => a.Service)
            .Include(a => a.Diagnoses)
            .Include(a => a.TreatmentPlans).ThenInclude(tp => tp.Service)
            .Include(a => a.Prescriptions).ThenInclude(p => p.Items)
            .Where(a => a.PatientId == patientId &&
                        (a.Status == AppointmentStatus.Completed || a.Status == AppointmentStatus.PendingPayment))
            .OrderByDescending(a => a.AppointmentDate)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Appointment>> GetCompletedHistoryForFamilyAsync(
        Guid primaryPatientId, Guid? filterPatientId, int take, CancellationToken cancellationToken = default)
    {
        return await dbContext.Appointments
            .Include(a => a.Dentist).ThenInclude(d => d.Employee).ThenInclude(e => e.User)
            .Include(a => a.Patient)
            .Include(a => a.Service)
            .Include(a => a.Diagnoses)
            .Include(a => a.TreatmentPlans).ThenInclude(tp => tp.Service)
            .Include(a => a.Prescriptions).ThenInclude(p => p.Items)
            .Where(a => (a.PatientId == primaryPatientId || a.Patient.PrimaryPatientId == primaryPatientId) &&
                        (filterPatientId == null || a.PatientId == filterPatientId) &&
                        (a.Status == AppointmentStatus.Completed || a.Status == AppointmentStatus.PendingPayment))
            .OrderByDescending(a => a.AppointmentDate)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Guid>> GetFollowUpChainAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        var chain = new HashSet<Guid> { appointmentId };
        bool added;
        do
        {
            added = false;

            var parents = await dbContext.Appointments
                .Where(a => chain.Contains(a.Id) && a.FollowUpFromAppointmentId != null)
                .Select(a => a.FollowUpFromAppointmentId!.Value)
                .ToListAsync(cancellationToken);
            foreach (var p in parents)
            {
                if (chain.Add(p)) added = true;
            }

            var children = await dbContext.Appointments
                .Where(a => a.FollowUpFromAppointmentId != null && chain.Contains(a.FollowUpFromAppointmentId.Value))
                .Select(a => a.Id)
                .ToListAsync(cancellationToken);
            foreach (var c in children)
            {
                if (chain.Add(c)) added = true;
            }
        } while (added);

        chain.Remove(appointmentId);
        return chain.ToList();
    }

    public async Task<IReadOnlyList<Appointment>> GetQueueAppointmentsByDateRangeAsync(DateTimeOffset utcStart, DateTimeOffset utcEnd, CancellationToken cancellationToken = default)
    {
        return await dbContext.Appointments
            .Include(a => a.Patient).ThenInclude(p => p.User)
            .Include(a => a.Dentist).ThenInclude(d => d.Employee).ThenInclude(e => e.User)
            .Include(a => a.Service)
            .Where(a => a.AppointmentDate >= utcStart && a.AppointmentDate < utcEnd &&
                        (a.Status == AppointmentStatus.CheckedIn ||
                         a.Status == AppointmentStatus.InProgress ||
                         a.Status == AppointmentStatus.Completed))
            .ToListAsync(cancellationToken);
    }

    public async Task<Appointment?> GetActiveTodayByPatientAsync(Guid patientId, DateTimeOffset utcStart, DateTimeOffset utcEnd, CancellationToken cancellationToken = default)
    {
        return await dbContext.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Dentist)
            .FirstOrDefaultAsync(a => a.PatientId == patientId &&
                                      a.AppointmentDate >= utcStart && a.AppointmentDate < utcEnd &&
                                      (a.Status == AppointmentStatus.CheckedIn || a.Status == AppointmentStatus.InProgress),
                                 cancellationToken);
    }

    public async Task<IReadOnlyList<Appointment>> GetByPatientAndDateRangeAsync(Guid patientId, DateTimeOffset utcStart, DateTimeOffset utcEnd, CancellationToken cancellationToken = default)
    {
        return await dbContext.Appointments
            .Where(a => a.PatientId == patientId && a.AppointmentDate >= utcStart && a.AppointmentDate < utcEnd)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Appointment>> GetFollowUpScheduledAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Appointments
            .AsNoTracking()
            .Include(a => a.Patient).ThenInclude(p => p.User)
            .Include(a => a.Dentist).ThenInclude(d => d.Employee).ThenInclude(e => e.User)
            .Include(a => a.Service)
            .Where(a => a.FollowUpDate != null &&
                        (a.Status == AppointmentStatus.Completed || a.Status == AppointmentStatus.PendingPayment))
            .ToListAsync(cancellationToken);
    }

    public async Task<HashSet<Guid>> GetCheckedInFollowUpOriginalIdsAsync(List<Guid> originalAppointmentIds, CancellationToken cancellationToken = default)
    {
        return (await dbContext.Appointments
            .AsNoTracking()
            .Where(f => f.FollowUpFromAppointmentId != null &&
                        originalAppointmentIds.Contains(f.FollowUpFromAppointmentId!.Value) &&
                        f.Status != AppointmentStatus.Cancelled)
            .Select(f => f.FollowUpFromAppointmentId!.Value)
            .ToListAsync(cancellationToken)).ToHashSet();
    }

    public async Task<Dictionary<Guid, Guid?>> GetFollowUpParentMapAsync(List<Guid> patientIds, CancellationToken cancellationToken = default)
    {
        return (await dbContext.Appointments
            .AsNoTracking()
            .Where(a => patientIds.Contains(a.PatientId))
            .Select(a => new { a.Id, a.FollowUpFromAppointmentId })
            .ToListAsync(cancellationToken))
            .ToDictionary(a => a.Id, a => a.FollowUpFromAppointmentId);
    }

    public async Task<bool> HasActiveFollowUpCheckInAsync(Guid originalAppointmentId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Appointments.AnyAsync(f =>
            f.FollowUpFromAppointmentId == originalAppointmentId && f.Status != AppointmentStatus.Cancelled, cancellationToken);
    }

    public async Task<Appointment?> GetForPrescriptionSuggestionAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Appointments
            .Include(a => a.Service)
            .Include(a => a.Diagnoses)
            .Include(a => a.TreatmentPlans).ThenInclude(tp => tp.Service)
            .Include(a => a.Prescriptions).ThenInclude(p => p.Items)
            .FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken);
    }

    public async Task<Appointment?> GetForTreatmentSuggestionAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Appointments
            .Include(a => a.Service)
            .Include(a => a.Diagnoses)
            .FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken);
    }

    public async Task<IReadOnlyList<Appointment>> GetPatientHistoryExcludingAsync(Guid patientId, Guid excludeAppointmentId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Appointments
            .Where(a => a.PatientId == patientId && a.Id != excludeAppointmentId)
            .Include(a => a.Service)
            .Include(a => a.Diagnoses)
            .Include(a => a.TreatmentPlans).ThenInclude(tp => tp.Service)
            .Include(a => a.Prescriptions).ThenInclude(p => p.Items)
            .OrderByDescending(a => a.AppointmentDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Appointment>> GetUpcomingForPatientsAsync(List<Guid> patientIds, DateTimeOffset fromUtc, int take, CancellationToken cancellationToken = default)
    {
        return await dbContext.Appointments
            .Include(a => a.Dentist).ThenInclude(d => d.Employee).ThenInclude(e => e.User)
            .Include(a => a.Patient).ThenInclude(p => p.User)
            .Where(a => patientIds.Contains(a.PatientId) &&
                (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed) &&
                a.AppointmentDate >= fromUtc)
            .OrderBy(a => a.AppointmentDate)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<Appointment?> GetNextUpcomingForPatientsAsync(List<Guid> patientIds, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken = default)
    {
        return await dbContext.Appointments
            .Include(a => a.Dentist)
            .Include(a => a.Patient)
            .Where(a => patientIds.Contains(a.PatientId) &&
                (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed) &&
                a.AppointmentDate >= fromUtc && a.AppointmentDate <= toUtc)
            .OrderBy(a => a.AppointmentDate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Appointment>> GetActiveByPatientOrUserAsync(Guid? patientId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Appointments
            .Include(a => a.Dentist)
            .Include(a => a.Patient)
            .AsNoTracking()
            .Where(a => ((patientId != null && a.PatientId == patientId) || a.Patient.UserId == userId)
                     && a.Status != AppointmentStatus.Cancelled)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountActiveAppointmentsForUserAsync(Guid userId, Guid? excludeAppointmentId = null, CancellationToken cancellationToken = default)
    {
        return await dbContext.Appointments
            .Where(a => (a.Patient.UserId == userId || (a.Patient.PrimaryPatient != null && a.Patient.PrimaryPatient.UserId == userId))
                     && (excludeAppointmentId == null || a.Id != excludeAppointmentId)
                     && a.Status != AppointmentStatus.Cancelled
                     && a.Status != AppointmentStatus.Completed
                     && a.Status != AppointmentStatus.NoShow)
            .CountAsync(cancellationToken);
    }

    public async Task<int> GetPatientCancellationCountAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Appointments
            .Where(a => a.PatientId == patientId
                     && a.Status == AppointmentStatus.Cancelled
                     && a.CancelledByUserId != null
                     && (a.CancelledByUserId == a.Patient.UserId || (a.Patient.PrimaryPatient != null && a.CancelledByUserId == a.Patient.PrimaryPatient.UserId)))
            .CountAsync(cancellationToken);
    }

    public async Task<int> GetPatientRescheduleCountAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Appointments
            .Where(a => a.PatientId == patientId)
            .SumAsync(a => a.RescheduledCount, cancellationToken);
    }

    public async Task<DateTimeOffset?> GetPatientCooldownUntilAsync(Guid patientId, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        DateTimeOffset? cooldownUntil = null;

        // 1. Kiểm tra cooldown do hủy lịch (khi bệnh nhân tự hủy >= 2 lần)
        var cancelCount = await GetPatientCancellationCountAsync(patientId, cancellationToken);
        if (cancelCount >= 2)
        {
            var latestCancel = await dbContext.Appointments
                .Where(a => a.PatientId == patientId
                         && a.Status == AppointmentStatus.Cancelled
                         && a.CancelledAt != null
                         && a.CancelledByUserId != null
                         && (a.CancelledByUserId == a.Patient.UserId || (a.Patient.PrimaryPatient != null && a.CancelledByUserId == a.Patient.PrimaryPatient.UserId)))
                .OrderByDescending(a => a.CancelledAt)
                .Select(a => a.CancelledAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (latestCancel.HasValue)
            {
                var candidate = latestCancel.Value.AddMinutes(30);
                if (candidate > now)
                {
                    cooldownUntil = candidate;
                }
            }
        }

        // 2. Kiểm tra cooldown do dời lịch (khi đã dời >= 2 lần)
        var rescheduleCount = await GetPatientRescheduleCountAsync(patientId, cancellationToken);
        if (rescheduleCount >= 2)
        {
            var latestReschedule = await dbContext.Appointments
                .Where(a => a.PatientId == patientId && a.LastRescheduledAt != null)
                .OrderByDescending(a => a.LastRescheduledAt)
                .Select(a => a.LastRescheduledAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (latestReschedule.HasValue)
            {
                var candidate = latestReschedule.Value.AddMinutes(30);
                if (candidate > now && (cooldownUntil == null || candidate > cooldownUntil.Value))
                {
                    cooldownUntil = candidate;
                }
            }
        }

        return cooldownUntil;
    }

    public async Task<bool> HasActiveAppointmentOnDateAsync(
        Guid accountOrPatientId,
        DateOnly date,
        Guid? excludeAppointmentId = null,
        CancellationToken cancellationToken = default)
    {
        var vnOffset = TimeSpan.FromHours(7);
        var startUtc = new DateTimeOffset(date.Year, date.Month, date.Day, 0, 0, 0, vnOffset).ToUniversalTime();
        var endUtc = startUtc.AddDays(1);

        return await dbContext.Appointments.AnyAsync(a =>
            (a.Patient.UserId == accountOrPatientId || a.PatientId == accountOrPatientId) &&
            (excludeAppointmentId == null || a.Id != excludeAppointmentId) &&
            a.AppointmentDate >= startUtc && a.AppointmentDate < endUtc &&
            a.Status != AppointmentStatus.Cancelled, cancellationToken);
    }
}
