using DentalClinic.API.Application.DTOs.Invoices;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static DentalClinic.API.Application.UseCases.Invoices.InvoiceHelpers;

namespace DentalClinic.API.Application.UseCases.Invoices;

public record ConfirmInvoicePaymentCommand(Guid InvoiceId, string? PaymentMethod) : IRequest<InvoiceDto>;

/// <summary>Xác nhận đã thanh toán hóa đơn → hoàn tất lịch hẹn / tất toán công nợ.</summary>
public class ConfirmInvoicePaymentHandler(
    AppDbContext dbContext,
    IPaymentConfirmationService paymentConfirmationService,
    InvoiceQueryHelper invoiceQuery) : IRequestHandler<ConfirmInvoicePaymentCommand, InvoiceDto>
{
    public async Task<InvoiceDto> Handle(ConfirmInvoicePaymentCommand command, CancellationToken ct)
    {
        var invoice = await dbContext.Invoices
            .Include(i => i.Appointment)
            .FirstOrDefaultAsync(i => i.Id == command.InvoiceId, ct)
            ?? throw new NotFoundException("Không tìm thấy hóa đơn.");

        if (invoice.Status == PaymentStatus.Paid)
            throw new ConflictException("Hóa đơn này đã được thanh toán.");

        var paymentMethod = string.IsNullOrWhiteSpace(command.PaymentMethod)
            ? invoice.PaymentMethod
            : ParsePaymentMethod(command.PaymentMethod);

        await paymentConfirmationService.ConfirmInvoicePaymentAsync(invoice, paymentMethod, ct);
        await dbContext.SaveChangesAsync(ct);

        return await invoiceQuery.GetByIdAsync(invoice.Id, ct);
    }
}
