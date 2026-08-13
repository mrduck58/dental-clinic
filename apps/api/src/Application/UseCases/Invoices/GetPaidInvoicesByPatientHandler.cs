using DentalClinic.API.Application.DTOs.Invoices;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Invoices;

public record GetPaidInvoicesByPatientQuery(Guid PatientId) : IRequest<List<InvoiceDto>>;

/// <summary>Hóa đơn đã thanh toán của một bệnh nhân cụ thể (mobile app — tab "Lịch sử giao dịch").</summary>
public class GetPaidInvoicesByPatientHandler(IInvoiceRepository invoiceRepository)
    : IRequestHandler<GetPaidInvoicesByPatientQuery, List<InvoiceDto>>
{
    public async Task<List<InvoiceDto>> Handle(GetPaidInvoicesByPatientQuery query, CancellationToken ct)
    {
        var invoices = await invoiceRepository.GetPaidInvoicesByPatientAsync(query.PatientId, ct);
        return invoices.Select(InvoiceHelpers.ToDto).ToList();
    }
}
