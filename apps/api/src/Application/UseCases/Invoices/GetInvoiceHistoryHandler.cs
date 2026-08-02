using DentalClinic.API.Application.DTOs.Invoices;
using DentalClinic.API.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Application.UseCases.Invoices;

public record GetInvoiceHistoryQuery : IRequest<List<InvoiceDto>>;

/// <summary>Tab "Lịch sử hóa đơn": các hóa đơn đã thanh toán.</summary>
public class GetInvoiceHistoryHandler(InvoiceQueryHelper invoiceQuery) : IRequestHandler<GetInvoiceHistoryQuery, List<InvoiceDto>>
{
    public async Task<List<InvoiceDto>> Handle(GetInvoiceHistoryQuery query, CancellationToken ct)
    {
        var invoices = await invoiceQuery.QueryWithDetails()
            .Where(i => i.Status == PaymentStatus.Paid)
            .OrderByDescending(i => i.PaymentDate)
            .ToListAsync(ct);

        return invoices.Select(InvoiceHelpers.ToDto).ToList();
    }
}
