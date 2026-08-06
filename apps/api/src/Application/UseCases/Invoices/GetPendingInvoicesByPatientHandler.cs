using DentalClinic.API.Application.DTOs.Invoices;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Invoices;

public record GetPendingInvoicesByPatientQuery(Guid PatientId) : IRequest<List<InvoiceDto>>;

/// <summary>Hóa đơn chưa thanh toán của một bệnh nhân cụ thể (mobile app — bệnh nhân xem hóa đơn của mình).</summary>
public class GetPendingInvoicesByPatientHandler(IInvoiceRepository invoiceRepository)
    : IRequestHandler<GetPendingInvoicesByPatientQuery, List<InvoiceDto>>
{
    public async Task<List<InvoiceDto>> Handle(GetPendingInvoicesByPatientQuery query, CancellationToken ct)
    {
        var invoices = await invoiceRepository.GetPendingInvoicesByPatientAsync(query.PatientId, ct);
        return invoices.Select(InvoiceHelpers.ToDto).ToList();
    }
}
