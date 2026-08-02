using DentalClinic.API.Application.DTOs.Invoices;
using DentalClinic.API.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Application.UseCases.Invoices;

public record GetPaidInvoicesByPatientQuery(Guid PatientId) : IRequest<List<InvoiceDto>>;

/// <summary>Hóa đơn đã thanh toán của một bệnh nhân cụ thể (mobile app — tab "Lịch sử giao dịch").</summary>
public class GetPaidInvoicesByPatientHandler(InvoiceQueryHelper invoiceQuery)
    : IRequestHandler<GetPaidInvoicesByPatientQuery, List<InvoiceDto>>
{
    public async Task<List<InvoiceDto>> Handle(GetPaidInvoicesByPatientQuery query, CancellationToken ct)
    {
        var invoices = await invoiceQuery.QueryWithDetails()
            .Where(i => i.Status == PaymentStatus.Paid && i.Appointment.PatientId == query.PatientId)
            .OrderByDescending(i => i.PaymentDate)
            .ToListAsync(ct);

        return invoices.Select(InvoiceHelpers.ToDto).ToList();
    }
}
