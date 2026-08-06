using DentalClinic.API.Application.DTOs.Invoices;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Invoices;

public record GetPendingInvoicesQuery : IRequest<List<InvoiceDto>>;

/// <summary>Tab "Chờ thanh toán": các hóa đơn chưa thanh toán.</summary>
public class GetPendingInvoicesHandler(IInvoiceRepository invoiceRepository) : IRequestHandler<GetPendingInvoicesQuery, List<InvoiceDto>>
{
    public async Task<List<InvoiceDto>> Handle(GetPendingInvoicesQuery query, CancellationToken ct)
    {
        var invoices = await invoiceRepository.GetPendingInvoicesAsync(ct);
        return invoices.Select(InvoiceHelpers.ToDto).ToList();
    }
}
