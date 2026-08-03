using DentalClinic.API.Application.DTOs.Invoices;
using DentalClinic.API.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Application.UseCases.Invoices;

public record GetPendingInvoicesQuery : IRequest<List<InvoiceDto>>;

/// <summary>Tab "Chờ thanh toán": các hóa đơn chưa thanh toán.</summary>
public class GetPendingInvoicesHandler(InvoiceQueryHelper invoiceQuery) : IRequestHandler<GetPendingInvoicesQuery, List<InvoiceDto>>
{
    public async Task<List<InvoiceDto>> Handle(GetPendingInvoicesQuery query, CancellationToken ct)
    {
        var invoices = await invoiceQuery.QueryWithDetails()
            .Where(i => i.Status == PaymentStatus.Unpaid)
            .OrderBy(i => i.CreatedAt)
            .ToListAsync(ct);

        return invoices.Select(InvoiceHelpers.ToDto).ToList();
    }
}
