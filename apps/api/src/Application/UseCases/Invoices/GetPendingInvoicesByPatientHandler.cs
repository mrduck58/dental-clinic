using DentalClinic.API.Application.DTOs.Invoices;
using DentalClinic.API.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Application.UseCases.Invoices;

public record GetPendingInvoicesByPatientQuery(Guid PatientId) : IRequest<List<InvoiceDto>>;

/// <summary>Hóa đơn chưa thanh toán của một bệnh nhân cụ thể (mobile app — bệnh nhân xem hóa đơn của mình).</summary>
public class GetPendingInvoicesByPatientHandler(InvoiceQueryHelper invoiceQuery)
    : IRequestHandler<GetPendingInvoicesByPatientQuery, List<InvoiceDto>>
{
    public async Task<List<InvoiceDto>> Handle(GetPendingInvoicesByPatientQuery query, CancellationToken ct)
    {
        var invoices = await invoiceQuery.QueryWithDetails()
            .Where(i => i.Status == PaymentStatus.Unpaid && i.Appointment.PatientId == query.PatientId)
            .OrderBy(i => i.CreatedAt)
            .ToListAsync(ct);

        return invoices.Select(InvoiceHelpers.ToDto).ToList();
    }
}
