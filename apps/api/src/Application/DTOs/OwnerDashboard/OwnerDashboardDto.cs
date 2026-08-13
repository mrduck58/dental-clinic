namespace DentalClinic.API.Application.DTOs.OwnerDashboard;

public record OwnerDashboardWeeklyTrendDto(
    string DateStr,
    string DayName,
    decimal Revenue,
    decimal Expense
);

public record OwnerOutstandingEmployeeDto(
    string Name,
    string Role,
    int Cases,
    double Rating,
    string Status
);

public record OwnerRatingBreakdownDto(
    double AverageRating,
    int TotalReviews,
    int FiveStar,
    int FourStar,
    int ThreeStar,
    int TwoStar,
    int OneStar
);

public record OwnerDashboardDto(
    decimal TotalRevenue,
    double RevenueGrowthPercent,
    decimal TotalExpense,
    double ExpenseGrowthPercent,
    // Chi phí mua vật tư (nhập kho) — thành phần con của TotalExpense, tách riêng cho tab "Doanh thu".
    decimal StockExpense,
    // Chi phí lương nhân viên (từ bảng lương đã chốt) — thành phần con còn lại của TotalExpense.
    decimal PayrollExpense,
    int NewPatientsCount,
    int NewPatientsThisWeekCount,
    List<OwnerDashboardWeeklyTrendDto> WeeklyTrend,
    OwnerRatingBreakdownDto RatingStats,
    List<OwnerOutstandingEmployeeDto> OutstandingEmployees
);
