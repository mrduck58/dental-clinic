using DentalClinic.API.Application.DTOs.Invoices;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Invoices;

public record CollectRemainingInvoiceCommand(Guid InvoiceId) : IRequest<InvoiceDto>;

/// <summary>Bắt đầu thu phần còn lại của một hóa đơn đặt cọc — đưa vào danh sách "Liệu trình → Hóa đơn".</summary>
public class CollectRemainingInvoiceHandler(IInvoiceRepository invoiceRepository, IUnitOfWork unitOfWork, InvoiceQueryHelper invoiceQuery)
    : IRequestHandler<CollectRemainingInvoiceCommand, InvoiceDto>
{
    public async Task<InvoiceDto> Handle(CollectRemainingInvoiceCommand command, CancellationToken ct)
    {
        var invoice = await invoiceRepository.GetByIdAsync(command.InvoiceId, ct)
            ?? throw new NotFoundException("Không tìm thấy hóa đơn.");

        if (invoice.IsSettled || invoice.RemainingAmount <= 0)
            throw new ValidationException("Hóa đơn này không còn công nợ để thu.");

        var alreadyHasChild = await invoiceRepository.HasChildInvoiceAsync(command.InvoiceId, ct);
        if (alreadyHasChild)
            throw new ConflictException("Đã tạo hóa đơn thu phần còn lại cho hóa đơn này.");

        invoice.StartCollectingRemaining();
        await unitOfWork.SaveChangesAsync(ct);

        return await invoiceQuery.GetByIdAsync(invoice.Id, ct);
    }
}
