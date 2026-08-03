using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Schedules;
using DentalClinic.API.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Application.UseCases.DentistDashboard;

public record DentistShiftDto(string Label, string Period, string? Room);
public record DentistWeekShiftsDto(int Total, int Morning, int Afternoon, int Evening);

public record DentistDashboardPatientDto(
    Guid AppointmentId,
    string PatientName,
    string? ServiceName,
    string Time,
    string Status);

public record DentistDashboardResponse(
    DateOnly Date,
    int TotalPatientsToday,
    int TotalWaiting,
    int TotalInProgress,
    int TotalCompleted,
    DentistWeekShiftsDto WeekShifts,
    List<DentistShiftDto> TodayShifts,
    List<DentistDashboardPatientDto> UpcomingPatients);

public record GetDentistDashboardQuery(Guid UserId) : IRequest<DentistDashboardResponse>;

public class DentistDashboardHandler(AppDbContext dbContext)
    : IRequestHandler<GetDentistDashboardQuery, DentistDashboardResponse>
{
    private static readonly TimeZoneInfo VietnamTz =
        TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

    public async Task<DentistDashboardResponse> Handle(GetDentistDashboardQuery request, CancellationToken ct)
    {
        var userId = request.UserId;

        var vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTz);
        var today = DateOnly.FromDateTime(vietnamNow);

        // Today's UTC boundaries
        var todayVnStart = new DateTimeOffset(today.Year, today.Month, today.Day, 0, 0, 0, VietnamTz.BaseUtcOffset);
        var utcStart = todayVnStart.ToUniversalTime();
        var utcEnd   = utcStart.AddDays(1);

        // Chỉ lấy data của chính bác sĩ đang đăng nhập
        var dentist = await dbContext.Dentists.FirstOrDefaultAsync(d => d.UserId == userId, ct);
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
        var todayAppointments = await dbContext.Appointments
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

        var weekSchedules = await dbContext.WorkSchedules
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

        // Upcoming patients: InProgress first, then CheckedIn, then Confirmed — top 5
        var upcomingPatients = todayAppointments
            .Where(a => a.Status == AppointmentStatus.InProgress  ||
                        a.Status == AppointmentStatus.CheckedIn   ||
                        a.Status == AppointmentStatus.Confirmed)
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
}
