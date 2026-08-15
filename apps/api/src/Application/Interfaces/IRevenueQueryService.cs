using DentalClinic.API.Application.DTOs.Revenue;

namespace DentalClinic.API.Application.Interfaces;

public record RevenueTransactionsFilter(
    DateOnly From,
    DateOnly To,
    Guid? DentistId,
    string? ServiceName,
    string? Status,
    string? PaymentMethod,
    string? Search,
    int Page,
    int PageSize,
    string? SortBy,
    string? SortDir);

public interface IRevenueQueryService
{
    Task<RevenueSummaryDto> GetSummaryAsync(DateOnly from, DateOnly to, CancellationToken ct);

    Task<RevenueTransactionsPagedDto> GetTransactionsPagedAsync(RevenueTransactionsFilter filter, CancellationToken ct);

    Task<RevenueChartsDto> GetChartsAsync(DateOnly from, DateOnly to, CancellationToken ct);
}
