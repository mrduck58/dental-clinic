using DentalClinic.API.Application.DTOs.OwnerDashboard;
using DentalClinic.API.Application.Interfaces;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Infrastructure.Services;

public class OwnerDashboardQueryService(AppDbContext db) : IOwnerDashboardQueryService
{
    private static readonly TimeZoneInfo VietnamTz =
        TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

    public async Task<OwnerDashboardDto> GetOwnerDashboardAsync(CancellationToken ct)
    {
        var nowVn = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, VietnamTz);
        var todayVn = DateOnly.FromDateTime(nowVn.DateTime);

        // Current month bounds (UTC)
        var startOfMonth = new DateTimeOffset(new DateTime(todayVn.Year, todayVn.Month, 1), TimeSpan.FromHours(7));
        var startOfNextMonth = startOfMonth.AddMonths(1);
        var startOfLastMonth = startOfMonth.AddMonths(-1);

        // Fetch lightweight projections for in-memory calculation (prevents Npgsql LINQ translation errors)
        var allInvoices = await db.Invoices
            .AsNoTracking()
            .Select(i => new { i.PaymentDate, i.DepositAmount, i.TotalAmount, i.Status })
            .ToListAsync(ct);

        var revenueCurrentMonth = allInvoices
            .Where(i => i.Status == PaymentStatus.Paid && i.PaymentDate >= startOfMonth && i.PaymentDate < startOfNextMonth)
            .Sum(i => i.DepositAmount > 0 ? i.DepositAmount : i.TotalAmount);

        var revenueLastMonth = allInvoices
            .Where(i => i.Status == PaymentStatus.Paid && i.PaymentDate >= startOfLastMonth && i.PaymentDate < startOfMonth)
            .Sum(i => i.DepositAmount > 0 ? i.DepositAmount : i.TotalAmount);

        var totalRevenueAllTime = allInvoices
            .Where(i => i.Status == PaymentStatus.Paid || i.DepositAmount > 0)
            .Sum(i => i.DepositAmount > 0 ? i.DepositAmount : i.TotalAmount);

        // Expense current vs last month
        var allPayrolls = await db.PayrollRecords
            .AsNoTracking()
            .Select(p => new { p.Year, p.Month, p.NetSalary })
            .ToListAsync(ct);

        var payrollExpenseCurrent = allPayrolls
            .Where(p => p.Year == todayVn.Year && p.Month == todayVn.Month)
            .Sum(p => p.NetSalary);

        var payrollExpenseLast = allPayrolls
            .Where(p => p.Year == startOfLastMonth.Year && p.Month == startOfLastMonth.Month)
            .Sum(p => p.NetSalary);

        var totalPayrollAllTime = allPayrolls.Sum(p => p.NetSalary);

        var allSupplyTransactions = await db.SupplyTransactions
            .AsNoTracking()
            .Select(t => new { t.Type, t.CreatedAt, Amount = (t.UnitPrice ?? 0m) * t.Quantity })
            .ToListAsync(ct);

        bool IsImport(string type) => string.Equals(type, "import", StringComparison.OrdinalIgnoreCase);

        var stockExpenseCurrent = allSupplyTransactions
            .Where(t => IsImport(t.Type) && t.CreatedAt >= startOfMonth && t.CreatedAt < startOfNextMonth)
            .Sum(t => t.Amount);

        var stockExpenseLast = allSupplyTransactions
            .Where(t => IsImport(t.Type) && t.CreatedAt >= startOfLastMonth && t.CreatedAt < startOfMonth)
            .Sum(t => t.Amount);

        var totalStockAllTime = allSupplyTransactions
            .Where(t => IsImport(t.Type))
            .Sum(t => t.Amount);

        var totalExpenseCurrent = payrollExpenseCurrent + stockExpenseCurrent;
        var totalExpenseLast = payrollExpenseLast + stockExpenseLast;
        var totalExpenseAllTime = totalPayrollAllTime + totalStockAllTime;

        // Growth percentages
        double revenueGrowth = CalcGrowthPercent(revenueCurrentMonth, revenueLastMonth);
        double expenseGrowth = CalcGrowthPercent(totalExpenseCurrent, totalExpenseLast);

        // Patients count
        var startOf7DaysAgo = new DateTimeOffset(todayVn.AddDays(-6).ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(7));
        var allPatients = await db.Patients
            .AsNoTracking()
            .Select(p => new { p.CreatedAt })
            .ToListAsync(ct);

        var totalPatientsAllTime = allPatients.Count;
        var newPatientsMonth = allPatients.Count(p => p.CreatedAt >= startOfMonth);
        var newPatientsWeek = allPatients.Count(p => p.CreatedAt >= startOf7DaysAgo);

        var displayPatientsCount = newPatientsMonth > 0 ? newPatientsMonth : totalPatientsAllTime;

        // Weekly Trend (last 7 days)
        var weeklyTrend = new List<OwnerDashboardWeeklyTrendDto>();
        var dayNames = new[] { "Chủ Nhật", "Thứ Hai", "Thứ Ba", "Thứ Tư", "Thứ Năm", "Thứ Sáu", "Thứ Bảy" };

        for (int i = 6; i >= 0; i--)
        {
            var d = todayVn.AddDays(-i);
            var dStart = new DateTimeOffset(d.ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(7));
            var dEnd = dStart.AddDays(1);

            var dayRevenue = allInvoices
                .Where(inv => (inv.Status == PaymentStatus.Paid || inv.DepositAmount > 0) && inv.PaymentDate >= dStart && inv.PaymentDate < dEnd)
                .Sum(inv => inv.DepositAmount > 0 ? inv.DepositAmount : inv.TotalAmount);

            var dayStockExpense = allSupplyTransactions
                .Where(t => IsImport(t.Type) && t.CreatedAt >= dStart && t.CreatedAt < dEnd)
                .Sum(t => t.Amount);

            weeklyTrend.Add(new OwnerDashboardWeeklyTrendDto(
                DateStr: $"{d.Day}/{d.Month}",
                DayName: dayNames[(int)d.DayOfWeek],
                Revenue: dayRevenue,
                Expense: dayStockExpense
            ));
        }

        // Ratings breakdown (from Feedback)
        var feedbacks = await db.Feedbacks.AsNoTracking().ToListAsync(ct);
        double avgRating = feedbacks.Count > 0
            ? Math.Round(feedbacks.Average(f => f.Rating), 1)
            : 5.0;

        int fiveStar = feedbacks.Count(f => f.Rating == 5);
        int fourStar = feedbacks.Count(f => f.Rating == 4);
        int threeStar = feedbacks.Count(f => f.Rating == 3);
        int twoStar = feedbacks.Count(f => f.Rating == 2);
        int oneStar = feedbacks.Count(f => f.Rating == 1);

        var ratingStats = new OwnerRatingBreakdownDto(
            AverageRating: avgRating,
            TotalReviews: feedbacks.Count,
            FiveStar: fiveStar,
            FourStar: fourStar,
            ThreeStar: threeStar,
            TwoStar: twoStar,
            OneStar: oneStar
        );

        // Outstanding Employees (Top Dentists + Employees by completed appointments and reviews)
        var dentists = await db.DentistProfiles
            .AsNoTracking()
            .Include(d => d.Employee)
                .ThenInclude(e => e.User)
            .ToListAsync(ct);

        var reviews = await db.DentistReviews.AsNoTracking().ToListAsync(ct);
        var completedAppts = await db.Appointments
            .AsNoTracking()
            .Where(a => a.Status == AppointmentStatus.Completed)
            .ToListAsync(ct);

        var outstandingEmployees = new List<OwnerOutstandingEmployeeDto>();

        foreach (var d in dentists)
        {
            var casesCount = completedAppts.Count(a => a.DentistId == d.Id);
            var dReviews = reviews.Where(r => r.DentistId == d.Id).ToList();
            double dRating = dReviews.Count > 0 ? Math.Round(dReviews.Average(r => r.Rating), 1) : 4.9;
            string docName = string.IsNullOrWhiteSpace(d.FullName)
                ? "Bác sĩ Nha khoa"
                : (d.FullName.StartsWith("BS.") ? d.FullName : $"BS. {d.FullName}");

            outstandingEmployees.Add(new OwnerOutstandingEmployeeDto(
                Name: docName,
                Role: string.IsNullOrWhiteSpace(d.Specialization) ? "Nha sĩ chuyên khoa" : d.Specialization,
                Cases: casesCount,
                Rating: dRating,
                Status: "Active"
            ));
        }

        if (outstandingEmployees.Count == 0)
        {
            var employees = await db.Employees
                .AsNoTracking()
                .Include(e => e.User)
                .Take(5)
                .ToListAsync(ct);

            foreach (var emp in employees)
            {
                string empName = emp.User?.FullName ?? "Nhân viên phòng khám";
                if (emp.Position?.ToLower().Contains("bác sĩ") == true || emp.User?.Role == UserRole.Dentist)
                {
                    if (!empName.StartsWith("BS.")) empName = $"BS. {empName}";
                }

                outstandingEmployees.Add(new OwnerOutstandingEmployeeDto(
                    Name: empName,
                    Role: string.IsNullOrWhiteSpace(emp.Position) ? "Nhân viên chuyên môn" : emp.Position,
                    Cases: 0,
                    Rating: 5.0,
                    Status: "Active"
                ));
            }
        }

        outstandingEmployees = outstandingEmployees
            .OrderByDescending(e => e.Cases)
            .ThenByDescending(e => e.Rating)
            .Take(5)
            .ToList();

        var finalRevenue = totalRevenueAllTime > 0 ? totalRevenueAllTime : revenueCurrentMonth;
        var finalExpense = totalExpenseCurrent > 0 ? totalExpenseCurrent : totalExpenseAllTime;

        return new OwnerDashboardDto(
            TotalRevenue: finalRevenue,
            RevenueGrowthPercent: revenueGrowth,
            TotalExpense: finalExpense,
            ExpenseGrowthPercent: expenseGrowth,
            NewPatientsCount: displayPatientsCount,
            NewPatientsThisWeekCount: newPatientsWeek,
            WeeklyTrend: weeklyTrend,
            RatingStats: ratingStats,
            OutstandingEmployees: outstandingEmployees
        );
    }

    private static double CalcGrowthPercent(decimal current, decimal previous)
    {
        if (previous == 0) return current == 0 ? 0 : 100;
        return (double)Math.Round((current - previous) / previous * 100, 1);
    }
}
