using DentalClinic.API.Application.DTOs.Invoices;
using DentalClinic.API.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Application.UseCases.Invoices;

public record GetOutstandingInvoicesQuery : IRequest<List<InvoiceDto>>;

/// <summary>
/// Tab "Công nợ": các hóa đơn chưa thu đủ — số tiền đã thu nhỏ hơn tổng tiền
/// (hóa đơn đặt cọc còn dư nợ). Không tính hóa đơn đã hoàn tiền.
/// </summary>
public class GetOutstandingInvoicesHandler(InvoiceQueryHelper invoiceQuery)
    : IRequestHandler<GetOutstandingInvoicesQuery, List<InvoiceDto>>
{
    public async Task<List<InvoiceDto>> Handle(GetOutstandingInvoicesQuery query, CancellationToken ct)
    {
        // Chỉ tính hóa đơn đặt cọc còn dư nợ, chưa tất toán (không tính hóa đơn con thu phần còn lại).
        var invoices = await invoiceQuery.QueryWithDetails()
            .Where(i => i.Status != PaymentStatus.Refunded
                        && !i.IsSettled
                        && i.ParentInvoiceId == null
                        && i.DepositAmount < i.TotalAmount)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(ct);

        return invoices.Select(InvoiceHelpers.ToDto).ToList();
    }
}
