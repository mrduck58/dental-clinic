using DentalClinic.API.Application.DTOs.Invoices;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Invoices;

public record GetInvoicesByPatientQuery(Guid PatientId) : IRequest<List<InvoiceDto>>;

/// <summary>Toàn bộ hóa đơn (chưa thanh toán + đã thanh toán) của một bệnh nhân — dùng cho màn hình
/// xem chi tiết bệnh nhân của chủ phòng khám/lễ tân/quản trị viên.</summary>
public class GetInvoicesByPatientHandler(IInvoiceRepository invoiceRepository)
    : IRequestHandler<GetInvoicesByPatientQuery, List<InvoiceDto>>
{
    public async Task<List<InvoiceDto>> Handle(GetInvoicesByPatientQuery query, CancellationToken ct)
    {
        var pending = await invoiceRepository.GetPendingInvoicesByPatientAsync(query.PatientId, ct);
        var paid = await invoiceRepository.GetPaidInvoicesByPatientAsync(query.PatientId, ct);

        return pending.Concat(paid)
            .Select(InvoiceHelpers.ToDto)
            .OrderByDescending(i => i.CreatedAt)
            .ToList();
    }
}
