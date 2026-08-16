using DentalClinic.API.Application.UseCases.DentistDashboard;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Application.Interfaces;
using DentalClinic.API.Domain.Schedules;
using DentalClinic.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Infrastructure.Services;

/// <summary>Đọc trực tiếp từ AppDbContext — truy vấn báo cáo/tổng hợp đa entity (Appointment,
/// DentistProfile, WorkSchedule) cho nhóm DentistDashboard. Logic chuyển verbatim từ các handler cũ.</summary>
public class DentistDashboardQueryService(AppDbContext db) : IDentistDashboardQueryService
{
    private static readonly TimeZoneInfo VietnamTz =
        TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

    public async Task<DentistDashboardResponse> GetDashboardAsync(Guid userId, CancellationToken ct)
    {
        var vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTz);
        var today = DateOnly.FromDateTime(vietnamNow);

        // Today's UTC boundaries
        var todayVnStart = new DateTimeOffset(today.Year, today.Month, today.Day, 0, 0, 0, VietnamTz.BaseUtcOffset);
        var utcStart = todayVnStart.ToUniversalTime();
        var utcEnd   = utcStart.AddDays(1);

        // Chỉ lấy data của chính bác sĩ đang đăng nhập
        var dentist = await db.DentistProfiles
            .Include(d => d.Employee).ThenInclude(e => e.User)
            .FirstOrDefaultAsync(d => d.Employee.UserId == userId, ct);
        if (dentist == null)
        {
            return new DentistDashboardResponse(
                today,
                TotalPatientsToday: 0,
                TotalWaiting:       0,
                TotalInProgress:    0,
                TotalCompleted:     0,
                WeekShifts:         new DentistWeekShiftsDto(0, 0, 0, 0),
                TodayShifts:        [],
                UpcomingPatients:   []);
        }

        // Today's appointments (all except Pending and Cancelled)
        var todayAppointments = await db.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Service)
            .Where(a => a.DentistId == dentist.Id &&
                        a.AppointmentDate >= utcStart &&
                        a.AppointmentDate < utcEnd &&
                        a.Status != AppointmentStatus.Pending &&
                        a.Status != AppointmentStatus.Cancelled)
            .OrderBy(a => a.AppointmentDate)
            .ToListAsync(ct);

        int totalToday      = todayAppointments.Count(a =>
            a.Status == AppointmentStatus.Confirmed      ||
            a.Status == AppointmentStatus.CheckedIn      ||
            a.Status == AppointmentStatus.InProgress     ||
            a.Status == AppointmentStatus.PendingPayment ||
            a.Status == AppointmentStatus.Completed);
        int totalWaiting    = todayAppointments.Count(a =>
            a.Status == AppointmentStatus.Confirmed ||
            a.Status == AppointmentStatus.CheckedIn);
        int totalInProgress = todayAppointments.Count(a => a.Status == AppointmentStatus.InProgress);
        int totalCompleted  = todayAppointments.Count(a =>
            a.Status == AppointmentStatus.Completed ||
            a.Status == AppointmentStatus.PendingPayment);

        // This week (Monday–Sunday)
        var dow = (int)today.DayOfWeek;
        var daysFromMon = dow == 0 ? 6 : dow - 1;
        var weekStart = today.AddDays(-daysFromMon);
        var weekEnd = weekStart.AddDays(7);

        var weekSchedules = await db.WorkSchedules
            .Where(s => s.StaffName == dentist.FullName &&
                        s.Date >= weekStart && s.Date < weekEnd &&
                        !s.IsHoliday)
            .ToListAsync(ct);

        var todaySchedules = weekSchedules.Where(s => s.Date == today).ToList();

        // Danh sách các ca NGANG HÀNG bác sĩ được phân hôm nay, xếp theo thời gian
        var todayShifts = todaySchedules
            .OrderBy(s => WorkShifts.SortKey(s.Shift))
            .Select(s => new DentistShiftDto(
                WorkShifts.LabelOf(s.Shift) ?? s.Shift,
                WorkShifts.PeriodOf(s.Shift),
                s.Room))
            .ToList();

        int weekMorning   = weekSchedules.Count(s => WorkShifts.PeriodOf(s.Shift) == WorkShifts.PeriodMorning);
        int weekAfternoon = weekSchedules.Count(s => WorkShifts.PeriodOf(s.Shift) == WorkShifts.PeriodAfternoon);
        int weekEvening   = weekSchedules.Count(s => WorkShifts.PeriodOf(s.Shift) == WorkShifts.PeriodEvening);

        // Upcoming patients: InProgress first, then CheckedIn (ordered by queue), then Confirmed — top 5
        var upcomingPatients = todayAppointments
            .Where(a => a.Status == AppointmentStatus.InProgress  ||
                        a.Status == AppointmentStatus.CheckedIn   ||
                        a.Status == AppointmentStatus.Confirmed)
            .OrderBy(a => a.Status switch
            {
                AppointmentStatus.InProgress => 0,
                AppointmentStatus.CheckedIn => 1,
                _ => 2
            })
            .ThenBy(a => a.QueueOrder ?? (a.CheckedInAt ?? a.AppointmentDate).UtcTicks)
            .ThenBy(a => a.Id)
            .Take(5)
            .Select(a =>
            {
                var vnTime = TimeZoneInfo.ConvertTime(a.AppointmentDate, VietnamTz);
                var status = a.Status switch
                {
                    AppointmentStatus.InProgress => "in_progress",
                    AppointmentStatus.CheckedIn  => "waiting",
                    _                            => "waiting"
                };
                return new DentistDashboardPatientDto(
                    a.Id,
                    a.Patient.FullName,
                    a.Service?.Name,
                    $"{vnTime.Hour:D2}:{vnTime.Minute:D2}",
                    status);
            })
            .ToList();

        return new DentistDashboardResponse(
            today,
            totalToday,
            totalWaiting,
            totalInProgress,
            totalCompleted,
            new DentistWeekShiftsDto(weekSchedules.Count, weekMorning, weekAfternoon, weekEvening),
            todayShifts,
            upcomingPatients);
    }

    public async Task<List<DentistPatientDto>?> GetPastPatientsAsync(Guid userId, CancellationToken ct)
    {
        var dentist = await db.DentistProfiles.FirstOrDefaultAsync(d => d.Employee.UserId == userId, ct);
        if (dentist == null) return null;

        var appointments = await db.Appointments
            .Include(a => a.Patient).ThenInclude(p => p.User)
            .Include(a => a.Service)
            .Where(a => a.DentistId == dentist.Id &&
                        (a.Status == AppointmentStatus.Completed || a.Status == AppointmentStatus.PendingPayment))
            .OrderByDescending(a => a.AppointmentDate)
            .ToListAsync(ct);

        return appointments.Select(a => new DentistPatientDto(
            a.Id,
            $"DK{a.AppointmentDate:yyyyMMdd}{a.Id.ToString("N")[..6].ToUpper()}",
            a.Patient.FullName,
            DentistPatientMapper.CalculateAge(a.Patient.DateOfBirth),
            a.Patient.Gender ?? "Khác",
            a.Patient.PhoneNumber ?? a.Patient.User?.PhoneNumber,
            a.AppointmentDate,
            a.Status.ToString(),
            a.Service?.Name,
            a.Symptoms,
            false,
            a.FollowUpFromAppointmentId != null
        )).ToList();
    }

    public async Task<DentistPatientsResponse> GetPatientsAsync(Guid dentistId, DateOnly date, CancellationToken ct)
    {
        var vietnamDateStart = new DateTimeOffset(date.Year, date.Month, date.Day, 0, 0, 0, VietnamTz.BaseUtcOffset);
        var utcStart = vietnamDateStart.ToUniversalTime();
        var utcEnd = utcStart.AddDays(1);
        var nowUtc = DateTimeOffset.UtcNow;

        var appointments = await db.Appointments
            .Include(a => a.Patient).ThenInclude(p => p.User)
            .Include(a => a.Service)
            .Where(a => a.DentistId == dentistId &&
                        a.AppointmentDate >= utcStart &&
                        a.AppointmentDate < utcEnd &&
                        // Chỉ hiện bệnh nhân đã check-in trở đi (đồng nhất với hàng đợi).
                        (a.Status == AppointmentStatus.CheckedIn    ||
                         a.Status == AppointmentStatus.InProgress   ||
                         a.Status == AppointmentStatus.PendingPayment ||
                         a.Status == AppointmentStatus.Completed))
            .ToListAsync(ct);

        // Số thứ tự đánh theo MỐC VÀO HÀNG ĐỢI (QueueEntryOrder ?? CheckedInAt ?? AppointmentDate)
        var stableNumbers = appointments
            .Where(a => a.Status is AppointmentStatus.CheckedIn
                                 or AppointmentStatus.InProgress
                                 or AppointmentStatus.Completed
                                 or AppointmentStatus.PendingPayment)
            .OrderBy(a => a.QueueEntryOrder ?? (a.CheckedInAt ?? a.AppointmentDate).UtcTicks)
            .ThenBy(a => a.Id)
            .Select((a, i) => (a.Id, Number: i + 1))
            .ToDictionary(x => x.Id, x => x.Number);

        // Sắp xếp thứ tự theo hàng đợi:
        // 1. InProgress (0) -> CheckedIn (1) -> PendingPayment / Completed (2)
        // 2. QueueOrder ?? CheckedInAt ?? AppointmentDate
        // 3. Id
        var orderedAppointments = appointments
            .OrderBy(a => a.Status switch
            {
                AppointmentStatus.InProgress => 0,
                AppointmentStatus.CheckedIn => 1,
                _ => 2
            })
            .ThenBy(a => a.QueueOrder ?? (a.CheckedInAt ?? a.AppointmentDate).UtcTicks)
            .ThenBy(a => a.Id)
            .ToList();

        var patients = orderedAppointments.Select(a => new DentistPatientDto(
            a.Id,
            $"DK{a.AppointmentDate:yyyyMMdd}{a.Id.ToString("N")[..6].ToUpper()}",
            a.Patient.FullName,
            DentistPatientMapper.CalculateAge(a.Patient.DateOfBirth),
            a.Patient.Gender ?? "Khác",
            a.Patient.PhoneNumber ?? a.Patient.User?.PhoneNumber,
            a.AppointmentDate,
            a.Status.ToString(),
            a.Service?.Name,
            a.Symptoms,
            IsNewPatient(a.PatientId),
            a.FollowUpFromAppointmentId != null,
            stableNumbers.GetValueOrDefault(a.Id, 0),
            a.CheckedInAt,
            a.Status == AppointmentStatus.CheckedIn
                ? Math.Max(0, (int)(nowUtc - (a.CheckedInAt ?? a.AppointmentDate)).TotalMinutes)
                : 0
        )).ToList();

        return new DentistPatientsResponse(
            date,
            appointments.Count(a => a.Status == AppointmentStatus.CheckedIn),
            appointments.Count(a => a.Status == AppointmentStatus.InProgress),
            appointments.Count(a => a.Status == AppointmentStatus.PendingPayment ||
                                   a.Status == AppointmentStatus.Completed),
            patients);
    }

    private bool IsNewPatient(Guid patientId)
    {
        return !db.Appointments.Any(a => a.PatientId == patientId && a.Status == AppointmentStatus.Completed);
    }

    public async Task<DentistPatientsResponse?> GetMyPatientsAsync(Guid userId, DateOnly? date, CancellationToken ct)
    {
        var dentist = await db.DentistProfiles.FirstOrDefaultAsync(d => d.Employee.UserId == userId, ct);
        if (dentist == null) return null;

        var vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTz);
        var queryDate = date ?? DateOnly.FromDateTime(vietnamNow);

        return await GetPatientsAsync(dentist.Id, queryDate, ct);
    }
}
