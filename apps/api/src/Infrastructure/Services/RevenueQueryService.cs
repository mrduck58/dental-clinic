using DentalClinic.API.Application.DTOs.Revenue;
using DentalClinic.API.Application.Interfaces;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Infrastructure.Services;

public class RevenueQueryService(AppDbContext db) : IRevenueQueryService
{
    private static readonly TimeZoneInfo VietnamTz =
        TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

    public async Task<RevenueSummaryDto> GetSummaryAsync(DateOnly from, DateOnly to, CancellationToken ct)
    {
        var (start, end) = PeriodBounds(from, to);

        // Tổng doanh thu (đã lập hóa đơn): chỉ tính hóa đơn gốc — hóa đơn "thu phần còn lại" chỉ tách nhỏ
        // cùng một khoản đã nằm trong TotalAmount của hóa đơn gốc, tính thêm sẽ đếm trùng.
        var totalBilled = await db.Invoices
            .AsNoTracking()
            .Where(i => i.ParentInvoiceId == null && i.CreatedAt >= start && i.CreatedAt < end)
            .SumAsync(i => i.TotalAmount, ct);

        // Đã thu: số tiền thực nhận trên MỌI hóa đơn đã thanh toán (gốc lẫn hóa đơn thu phần còn lại) —
        // DepositAmount luôn là số tiền thu ngay trên chính hóa đơn đó, dù là cọc hay thu nốt.
        var totalCollected = await db.Invoices
            .AsNoTracking()
            .Where(i => i.Status == PaymentStatus.Paid && i.PaymentDate >= start && i.PaymentDate < end)
            .SumAsync(i => i.DepositAmount, ct);

        // Chưa thu = hóa đơn chưa thanh toán (kể cả hóa đơn thu phần còn lại chưa trả)
        //          + phần còn nợ của hóa đơn cọc đã thu nhưng CHƯA có hóa đơn con thu nốt được tạo.
        var uncollectedFromUnpaid = await db.Invoices
            .AsNoTracking()
            .Where(i => i.Status == PaymentStatus.Unpaid && i.CreatedAt >= start && i.CreatedAt < end)
            .SumAsync(i => i.TotalAmount, ct);

        var uncollectedFromUnsettledDeposits = await db.Invoices
            .AsNoTracking()
            .Where(i => i.Status == PaymentStatus.Paid && !i.IsSettled && i.TotalAmount > i.DepositAmount
                && i.PaymentDate >= start && i.PaymentDate < end
                && !db.Invoices.Any(c => c.ParentInvoiceId == i.Id))
            .SumAsync(i => i.TotalAmount - i.DepositAmount, ct);

        var totalRefunded = await db.Invoices
            .AsNoTracking()
            .Where(i => i.Status == PaymentStatus.Refunded && i.PaymentDate >= start && i.PaymentDate < end)
            .SumAsync(i => i.TotalAmount, ct);

        return new RevenueSummaryDto(
            totalBilled,
            totalCollected,
            uncollectedFromUnpaid + uncollectedFromUnsettledDeposits,
            totalRefunded);
    }

    public async Task<RevenueTransactionsPagedDto> GetTransactionsPagedAsync(RevenueTransactionsFilter filter, CancellationToken ct)
    {
        var (start, end) = PeriodBounds(filter.From, filter.To);

        var query = db.Invoices
            .AsNoTracking()
            .Include(i => i.Appointment).ThenInclude(a => a.Patient).ThenInclude(p => p.User)
            .Include(i => i.Appointment).ThenInclude(a => a.Dentist).ThenInclude(d => d.Employee).ThenInclude(e => e.User)
            .Include(i => i.Items)
            .Where(i => i.CreatedAt >= start && i.CreatedAt < end)
            .AsQueryable();

        if (filter.DentistId is Guid dentistId)
            query = query.Where(i => i.Appointment.DentistId == dentistId);

        if (!string.IsNullOrWhiteSpace(filter.ServiceName))
            query = query.Where(i => i.Items.Any(it => it.Name == filter.ServiceName));

        if (!string.IsNullOrWhiteSpace(filter.Status)
            && Enum.TryParse<PaymentStatus>(filter.Status, true, out var status))
            query = query.Where(i => i.Status == status);

        if (!string.IsNullOrWhiteSpace(filter.PaymentMethod)
            && Enum.TryParse<PaymentMethod>(filter.PaymentMethod, true, out var method))
            query = query.Where(i => i.PaymentMethod == method);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(i =>
                i.InvoiceNumber.Contains(term)
                || i.Appointment.Patient.User.FullName.Contains(term)
                || i.Appointment.Dentist.Employee.User.FullName.Contains(term));
        }

        var totalCount = await query.CountAsync(ct);

        var sortDir = string.Equals(filter.SortDir, "asc", StringComparison.OrdinalIgnoreCase) ? "asc" : "desc";
        query = filter.SortBy switch
        {
            "amount" => sortDir == "asc"
                ? query.OrderBy(i => i.Status == PaymentStatus.Paid ? i.DepositAmount : i.TotalAmount)
                : query.OrderByDescending(i => i.Status == PaymentStatus.Paid ? i.DepositAmount : i.TotalAmount),
            "patient" => sortDir == "asc"
                ? query.OrderBy(i => i.Appointment.Patient.User.FullName)
                : query.OrderByDescending(i => i.Appointment.Patient.User.FullName),
            "dentist" => sortDir == "asc"
                ? query.OrderBy(i => i.Appointment.Dentist.Employee.User.FullName)
                : query.OrderByDescending(i => i.Appointment.Dentist.Employee.User.FullName),
            _ => sortDir == "asc"
                ? query.OrderBy(i => i.CreatedAt)
                : query.OrderByDescending(i => i.CreatedAt),
        };

        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize < 1 ? 20 : filter.PageSize;

        var rows = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(i => new
            {
                i.Id,
                i.InvoiceNumber,
                PatientId = i.Appointment.Patient.Id,
                PatientName = i.Appointment.Patient.User.FullName,
                DentistId = i.Appointment.DentistId,
                DentistName = i.Appointment.Dentist.Employee.User.FullName,
                ItemNames = i.Items.Select(it => it.Name).ToList(),
                i.CreatedAt,
                i.PaymentMethod,
                i.Status,
                i.TotalAmount,
                i.DepositAmount,
            })
            .ToListAsync(ct);

        var items = rows.Select(r => new RevenueTransactionDto(
            r.Id,
            r.InvoiceNumber,
            r.PatientId,
            r.PatientName ?? "Bệnh nhân",
            DescribeServices(r.ItemNames),
            r.DentistId,
            r.DentistName ?? "Bác sĩ",
            r.CreatedAt,
            DescribePaymentMethod(r.PaymentMethod),
            r.Status == PaymentStatus.Paid ? r.DepositAmount : r.TotalAmount,
            DescribeStatus(r.Status)))
            .ToList();

        return new RevenueTransactionsPagedDto(
            items,
            totalCount,
            page,
            pageSize,
            Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize)));
    }

    public async Task<RevenueChartsDto> GetChartsAsync(DateOnly from, DateOnly to, CancellationToken ct)
    {
        var (start, end) = PeriodBounds(from, to);

        // GroupBy qua navigation property không dịch nhất quán ở mọi provider (đặc biệt InMemory dùng cho
        // test) — lấy dữ liệu thô về trước rồi nhóm/tổng hợp ở phía client cho chắc chắn.
        var paidItems = await db.InvoiceItems
            .AsNoTracking()
            .Include(it => it.Invoice)
            .Where(it => it.Invoice.Status == PaymentStatus.Paid
                && it.Invoice.PaymentDate >= start && it.Invoice.PaymentDate < end)
            .Select(it => new { it.Name, it.AmountCollected })
            .ToListAsync(ct);

        var byService = paidItems
            .GroupBy(it => it.Name)
            .Select(g => new RevenueByServiceDto(g.Key, g.Sum(it => it.AmountCollected)))
            .OrderByDescending(x => x.Amount)
            .Take(10)
            .ToList();

        var paidInvoices = await db.Invoices
            .AsNoTracking()
            .Include(i => i.Appointment).ThenInclude(a => a.Dentist).ThenInclude(d => d.Employee).ThenInclude(e => e.User)
            .Where(i => i.Status == PaymentStatus.Paid && i.PaymentDate >= start && i.PaymentDate < end)
            .Select(i => new { i.Appointment.DentistId, DentistName = i.Appointment.Dentist.Employee.User.FullName, i.DepositAmount })
            .ToListAsync(ct);

        var byDentist = paidInvoices
            .GroupBy(i => new { i.DentistId, i.DentistName })
            .Select(g => new RevenueByDentistDto(g.Key.DentistId, g.Key.DentistName ?? "Bác sĩ", g.Sum(i => i.DepositAmount)))
            .OrderByDescending(x => x.Amount)
            .Take(10)
            .ToList();

        return new RevenueChartsDto(byService, byDentist);
    }

    private (DateTimeOffset Start, DateTimeOffset End) PeriodBounds(DateOnly from, DateOnly to)
    {
        var (fromDate, toDate) = from > to ? (to, from) : (from, to);
        return (ToVn(fromDate), ToVn(toDate.AddDays(1)));
    }

    private DateTimeOffset ToVn(DateOnly date)
        => new DateTimeOffset(date.Year, date.Month, date.Day, 0, 0, 0, VietnamTz.BaseUtcOffset).ToUniversalTime();

    // Thứ tự trong Items không được đảm bảo ổn định (collection navigation, không có cột sắp xếp) —
    // sắp theo tên để hiển thị nhất quán giữa các lần load.
    private static string DescribeServices(List<string> names) => names.Count switch
    {
        0 => "—",
        1 => names[0],
        _ => names.OrderBy(n => n, StringComparer.Ordinal).ToArray() is var sorted
            ? $"{sorted[0]} +{sorted.Length - 1} dịch vụ khác"
            : "—",
    };

    private static string DescribePaymentMethod(PaymentMethod method) => method switch
    {
        PaymentMethod.Cash => "Tiền mặt",
        PaymentMethod.BankTransfer => "Chuyển khoản",
        PaymentMethod.OnlinePayment => "Thanh toán online",
        _ => method.ToString(),
    };

    private static string DescribeStatus(PaymentStatus status) => status.ToString();
}
