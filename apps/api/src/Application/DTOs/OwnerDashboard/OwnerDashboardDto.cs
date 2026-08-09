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
    int NewPatientsCount,
    int NewPatientsThisWeekCount,
    List<OwnerDashboardWeeklyTrendDto> WeeklyTrend,
    OwnerRatingBreakdownDto RatingStats,
    List<OwnerOutstandingEmployeeDto> OutstandingEmployees
);
