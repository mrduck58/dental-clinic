namespace DentalClinic.API.Application.DTOs.OwnerDashboard;

/// <summary>1 dòng thu — hóa đơn đã thanh toán của bệnh nhân.</summary>
public record OwnerRevenueIncomeItemDto(
    Guid InvoiceId,
    string InvoiceNumber,
    string PatientName,
    string DentistName,
    string PaymentMethod,
    decimal Amount,
    DateTimeOffset Date);

/// <summary>1 dòng chi — nhập kho vật tư, hoặc bảng lương nhân viên.</summary>
public record OwnerRevenueExpenseItemDto(
    Guid Id,
    string Category, // "supply" | "payroll"
    string Description,
    string SubDescription,
    string Status,
    decimal Amount,
    DateTimeOffset Date);

public record OwnerRevenueReportDto(
    DateTimeOffset PeriodStart,
    DateTimeOffset PeriodEnd,
    decimal TotalIncome,
    decimal TotalStockExpense,
    decimal TotalPayrollExpense,
    List<OwnerRevenueIncomeItemDto> IncomeItems,
    List<OwnerRevenueExpenseItemDto> ExpenseItems);
