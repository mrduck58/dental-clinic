using DentalClinic.API.Application.DTOs.StaffDashboard;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Application.Interfaces;
using DentalClinic.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using static DentalClinic.API.Application.UseCases.StaffDashboard.StaffDashboardDateHelper;

namespace DentalClinic.API.Infrastructure.Services;

/// <summary>Đọc trực tiếp từ AppDbContext — truy vấn báo cáo/tổng hợp đa entity (Appointment, Invoice)
/// cho nhóm StaffDashboard. Logic chuyển verbatim từ các handler cũ.</summary>
public class StaffDashboardQueryService(AppDbContext db) : IStaffDashboardQueryService
{
    private static readonly AppointmentStatus[] ActiveTodayStatuses =
    [
        AppointmentStatus.Confirmed,
        AppointmentStatus.CheckedIn,
        AppointmentStatus.InProgress
    ];

    public async Task<StaffDashboardStatsDto> GetStatsAsync(CancellationToken ct)
    {
        var (start, end) = TodayVnRange();

        var appointmentsTodayCount = await db.Appointments.CountAsync(
            a => a.AppointmentDate >= start && a.AppointmentDate < end && a.Status != AppointmentStatus.Cancelled, ct);

        var waitingCheckInCount = await db.Appointments.CountAsync(
            a => a.AppointmentDate >= start && a.AppointmentDate < end && a.Status == AppointmentStatus.Confirmed, ct);

        var inProgressCount = await db.Appointments.CountAsync(
            a => a.AppointmentDate >= start && a.AppointmentDate < end && a.Status == AppointmentStatus.InProgress, ct);

        var pendingInvoicesCount = await db.Invoices.CountAsync(i => i.Status == PaymentStatus.Unpaid, ct);

        return new StaffDashboardStatsDto(appointmentsTodayCount, waitingCheckInCount, inProgressCount, pendingInvoicesCount);
    }

    public async Task<IReadOnlyList<StaffTodayAppointmentDto>> GetTodayAppointmentsAsync(int limit, CancellationToken ct)
    {
        var clampedLimit = Math.Clamp(limit, 1, 50);
        var (start, end) = TodayVnRange();

        return await db.Appointments
            .AsNoTracking()
            .Where(a => a.AppointmentDate >= start && a.AppointmentDate < end
                        && ActiveTodayStatuses.Contains(a.Status))
            .OrderBy(a => a.AppointmentDate)
            .Take(clampedLimit)
            .Select(a => new StaffTodayAppointmentDto(
                a.Id,
                a.Patient.User.FullName ?? string.Empty,
                a.Service != null ? a.Service.Name : null,
                a.Dentist.Employee.User.FullName ?? string.Empty,
                a.AppointmentDate,
                a.Status.ToString()))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<StaffPendingInvoiceDto>> GetPendingInvoicesAsync(int limit, CancellationToken ct)
    {
        var clampedLimit = Math.Clamp(limit, 1, 50);

        var invoices = await db.Invoices
            .AsNoTracking()
            .Include(i => i.Items)
            .Include(i => i.Appointment).ThenInclude(a => a.Patient).ThenInclude(p => p.User)
            .Where(i => i.Status == PaymentStatus.Unpaid)
            .OrderBy(i => i.CreatedAt)
            .Take(clampedLimit)
            .ToListAsync(ct);

        return invoices
            .Select(i => new StaffPendingInvoiceDto(
                i.Id,
                i.InvoiceNumber,
                i.Appointment.Patient.FullName,
                i.Items.FirstOrDefault()?.Name,
                i.TotalAmount))
            .ToList();
    }
}
