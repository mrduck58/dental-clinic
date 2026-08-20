namespace DentalClinic.API.Application.DTOs.Revenue;

public record RevenueSummaryDto(
    decimal TotalBilled,
    decimal TotalCollected,
    decimal TotalUncollected,
    decimal TotalRefunded);

public record RevenueTransactionDto(
    Guid InvoiceId,
    string InvoiceNumber,
    Guid PatientId,
    string PatientName,
    string ServiceSummary,
    Guid DentistId,
    string DentistName,
    DateTimeOffset Date,
    string PaymentMethod,
    decimal Amount,
    string Status,
    // > 0 chỉ khi đây là hóa đơn đặt cọc (Paid) đã thu Amount nhưng còn nợ phần này —
    // và CHƯA có hóa đơn "thu phần còn lại" nào được tạo cho nó.
    decimal RemainingAmount);

public record RevenueTransactionsPagedDto(
    IReadOnlyList<RevenueTransactionDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);

// SupplyCost = giá vốn vật tư đã tiêu hao thực tế cho dịch vụ này trong kỳ (xem TreatmentSupplyUsage) —
// khớp theo TÊN dịch vụ vì ServiceName ở đây vốn lấy từ InvoiceItem.Name (không có FK ServiceId thật),
// nên chỉ ước lượng tương đối khi có hai dịch vụ trùng tên.
public record RevenueByServiceDto(string ServiceName, decimal Amount, decimal SupplyCost);

public record RevenueByDentistDto(Guid DentistId, string DentistName, decimal Amount);

public record RevenueChartsDto(
    IReadOnlyList<RevenueByServiceDto> ByService,
    IReadOnlyList<RevenueByDentistDto> ByDentist);
