using DentalClinic.API.Application.DTOs.Dashboard;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static DentalClinic.API.Application.UseCases.Dashboard.DashboardDateHelper;

namespace DentalClinic.API.Application.UseCases.Dashboard;

public record GetDashboardStatsQuery(string? Range) : IRequest<DashboardStatsDto>;

/// <summary>Đọc trực tiếp từ AppDbContext — truy vấn báo cáo/tổng hợp đa entity (Appointment, Invoice, Patient).</summary>
public class GetDashboardStatsHandler(AppDbContext dbContext) : IRequestHandler<GetDashboardStatsQuery, DashboardStatsDto>
{
    public async Task<DashboardStatsDto> Handle(GetDashboardStatsQuery query, CancellationToken ct)
    {
        var normalizedRange = NormalizeRange(query.Range);
        var today = GetVietnamToday();
        var (currentStart, currentEnd) = GetCurrentPeriodDates(normalizedRange, today);
        var (previousStart, previousEnd) = GetPreviousPeriodDates(normalizedRange, currentStart);

        var currentStartOffset = ToVn(currentStart);
        var currentEndOffset = ToVn(currentEnd);
        var previousStartOffset = ToVn(previousStart);
        var previousEndOffset = ToVn(previousEnd);

        var newPatientsCurrent = await dbContext.Patients
            .CountAsync(p => p.CreatedAt >= currentStartOffset && p.CreatedAt < currentEndOffset, ct);
        var newPatientsPrevious = await dbContext.Patients
            .CountAsync(p => p.CreatedAt >= previousStartOffset && p.CreatedAt < previousEndOffset, ct);

        var appointmentsCurrent = await CountAppointmentsAsync(currentStartOffset, currentEndOffset, ct);
        var appointmentsPrevious = await CountAppointmentsAsync(previousStartOffset, previousEndOffset, ct);

        var revenueCurrent = await SumRevenueAsync(currentStartOffset, currentEndOffset, ct);
        var revenuePrevious = await SumRevenueAsync(previousStartOffset, previousEndOffset, ct);

        return new DashboardStatsDto(
            normalizedRange,
            currentStartOffset,
            currentEndOffset,
            newPatientsCurrent,
            CalcTrendPercent(newPatientsCurrent, newPatientsPrevious),
            appointmentsCurrent,
            CalcTrendPercent(appointmentsCurrent, appointmentsPrevious),
            revenueCurrent,
            CalcTrendPercent(revenueCurrent, revenuePrevious));
    }

    private async Task<int> CountAppointmentsAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken ct) =>
        await dbContext.Appointments.CountAsync(
            a => a.AppointmentDate >= start && a.AppointmentDate < end && a.Status != AppointmentStatus.Cancelled, ct);

    /// <summary>Doanh thu thực thu = DepositAmount (số tiền thu trên hóa đơn) của các hóa đơn đã thanh toán trong kỳ.</summary>
    private async Task<decimal> SumRevenueAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken ct) =>
        await dbContext.Invoices
            .Where(i => i.Status == PaymentStatus.Paid && i.PaymentDate >= start && i.PaymentDate < end)
            .SumAsync(i => (decimal?)i.DepositAmount, ct) ?? 0m;

    private static double CalcTrendPercent(int current, int previous) => CalcTrendPercent((decimal)current, (decimal)previous);

    private static double CalcTrendPercent(decimal current, decimal previous)
    {
        if (previous == 0) return current == 0 ? 0 : 100;
        return (double)Math.Round((current - previous) / previous * 100, 1);
    }
}
