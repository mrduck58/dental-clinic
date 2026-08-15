using DentalClinic.API.Application.DTOs.Revenue;
using DentalClinic.API.Application.Interfaces;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Revenue;

public record GetRevenueTransactionsPagedQuery(
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
    string? SortDir) : IRequest<RevenueTransactionsPagedDto>;

public class GetRevenueTransactionsPagedHandler(IRevenueQueryService revenueQueryService)
    : IRequestHandler<GetRevenueTransactionsPagedQuery, RevenueTransactionsPagedDto>
{
    public Task<RevenueTransactionsPagedDto> Handle(GetRevenueTransactionsPagedQuery request, CancellationToken ct)
        => revenueQueryService.GetTransactionsPagedAsync(
            new RevenueTransactionsFilter(
                request.From,
                request.To,
                request.DentistId,
                request.ServiceName,
                request.Status,
                request.PaymentMethod,
                request.Search,
                request.Page,
                request.PageSize,
                request.SortBy,
                request.SortDir),
            ct);
}
