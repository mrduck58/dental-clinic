using DentalClinic.API.Application.DTOs.Invoices;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Invoices;

public record GetInvoiceHistoryQuery : IRequest<List<InvoiceDto>>;

/// <summary>Tab "Lịch sử hóa đơn": các hóa đơn đã thanh toán.</summary>
public class GetInvoiceHistoryHandler(IInvoiceRepository invoiceRepository) : IRequestHandler<GetInvoiceHistoryQuery, List<InvoiceDto>>
{
    public async Task<List<InvoiceDto>> Handle(GetInvoiceHistoryQuery query, CancellationToken ct)
    {
        var invoices = await invoiceRepository.GetInvoiceHistoryAsync(ct);
        return invoices.Select(InvoiceHelpers.ToDto).ToList();
    }
}
