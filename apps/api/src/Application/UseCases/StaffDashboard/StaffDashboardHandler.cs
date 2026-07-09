using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Application.UseCases.StaffDashboard;

#region DTOs & Requests

/// <summary>4 chỉ số chính của Dashboard Staff: lịch hẹn hôm nay, chờ check-in, đang khám, chờ thanh toán.</summary>
public record StaffDashboardStatsDto(
    int AppointmentsTodayCount,
    int WaitingCheckInCount,
    int InProgressCount,
    int PendingInvoicesCount);

public record StaffTodayAppointmentDto(
    Guid Id,
    string PatientName,
    string? ServiceName,
    string DentistName,
    DateTimeOffset AppointmentDate,
    string Status);

public record StaffPendingInvoiceDto(
    Guid Id,
    string InvoiceNumber,
    string PatientName,
    string? ServiceName,
    decimal Amount);

#endregion

/// <summary>
/// Tổng hợp dữ liệu cho trang Dashboard Staff (tổng quan lễ tân).
/// Đọc trực tiếp từ AppDbContext — cùng phong cách với DashboardHandler (Admin),
/// DentistDashboardHandler và InvoiceHandler cho các truy vấn báo cáo đa entity.
/// </summary>
public class StaffDashboardHandler(AppDbContext dbContext)
{
    private static readonly TimeZoneInfo VietnamTz =
        TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

    private static readonly AppointmentStatus[] ActiveTodayStatuses =
    [
        AppointmentStatus.Confirmed,
        AppointmentStatus.CheckedIn,
        AppointmentStatus.InProgress
    ];

    public async Task<StaffDashboardStatsDto> GetStatsAsync(CancellationToken ct = default)
    {
        var (start, end) = TodayVnRange();

        var appointmentsTodayCount = await dbContext.Appointments.CountAsync(
            a => a.AppointmentDate >= start && a.AppointmentDate < end && a.Status != AppointmentStatus.Cancelled, ct);

        var waitingCheckInCount = await dbContext.Appointments.CountAsync(
            a => a.AppointmentDate >= start && a.AppointmentDate < end && a.Status == AppointmentStatus.Confirmed, ct);

        var inProgressCount = await dbContext.Appointments.CountAsync(
            a => a.AppointmentDate >= start && a.AppointmentDate < end && a.Status == AppointmentStatus.InProgress, ct);

        var pendingInvoicesCount = await dbContext.Invoices.CountAsync(i => i.Status == PaymentStatus.Unpaid, ct);

        return new StaffDashboardStatsDto(appointmentsTodayCount, waitingCheckInCount, inProgressCount, pendingInvoicesCount);
    }

    /// <summary>Lịch hẹn hôm nay đang ở trạng thái cần staff theo dõi (đã xác nhận / đã check-in / đang khám).</summary>
    public async Task<IReadOnlyList<StaffTodayAppointmentDto>> GetTodayAppointmentsAsync(
        int limit, CancellationToken ct = default)
    {
        var clampedLimit = Math.Clamp(limit, 1, 50);
        var (start, end) = TodayVnRange();

        return await dbContext.Appointments
            .AsNoTracking()
            .Where(a => a.AppointmentDate >= start && a.AppointmentDate < end
                        && ActiveTodayStatuses.Contains(a.Status))
            .OrderBy(a => a.AppointmentDate)
            .Take(clampedLimit)
            .Select(a => new StaffTodayAppointmentDto(
                a.Id,
                a.Patient.FullName,
                a.Service != null ? a.Service.Name : null,
                a.Dentist.FullName,
                a.AppointmentDate,
                a.Status.ToString()))
            .ToListAsync(ct);
    }

    /// <summary>Hóa đơn chưa thanh toán, cũ nhất trước — khớp thứ tự với tab "Chờ thanh toán".</summary>
    public async Task<IReadOnlyList<StaffPendingInvoiceDto>> GetPendingInvoicesAsync(
        int limit, CancellationToken ct = default)
    {
        var clampedLimit = Math.Clamp(limit, 1, 50);

        var invoices = await dbContext.Invoices
            .AsNoTracking()
            .Include(i => i.Items)
            .Include(i => i.Appointment).ThenInclude(a => a.Patient)
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

    private static (DateTimeOffset Start, DateTimeOffset End) TodayVnRange()
    {
        var vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTz);
        var today = DateOnly.FromDateTime(vietnamNow);

        // Nửa đêm (giờ VN) quy về UTC — Npgsql chỉ chấp nhận DateTimeOffset với Offset=0
        // khi ghi vào cột "timestamp with time zone".
        var start = new DateTimeOffset(today.Year, today.Month, today.Day, 0, 0, 0, VietnamTz.BaseUtcOffset)
            .ToUniversalTime();
        return (start, start.AddDays(1));
    }
}
