using DentalClinic.API.Application.DTOs.Invoices;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Invoices;

public record GetOutstandingInvoicesQuery : IRequest<List<InvoiceDto>>;

/// <summary>
/// Tab "Công nợ": các hóa đơn chưa thu đủ — số tiền đã thu nhỏ hơn tổng tiền
/// (hóa đơn đặt cọc còn dư nợ). Không tính hóa đơn đã hoàn tiền.
/// </summary>
public class GetOutstandingInvoicesHandler(IInvoiceRepository invoiceRepository)
    : IRequestHandler<GetOutstandingInvoicesQuery, List<InvoiceDto>>
{
    public async Task<List<InvoiceDto>> Handle(GetOutstandingInvoicesQuery query, CancellationToken ct)
    {
        var invoices = await invoiceRepository.GetOutstandingInvoicesAsync(ct);
        return invoices.Select(InvoiceHelpers.ToDto).ToList();
    }
}
