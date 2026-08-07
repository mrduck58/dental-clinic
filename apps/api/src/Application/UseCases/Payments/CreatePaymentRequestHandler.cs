using DentalClinic.API.Application.DTOs.Payments;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Configuration;
using static DentalClinic.API.Application.UseCases.Payments.PaymentHelpers;

namespace DentalClinic.API.Application.UseCases.Payments;

/// <param name="RestrictToUserId">
/// Khi người gọi là bệnh nhân: id tài khoản của họ — hóa đơn phải thuộc hồ sơ chính chủ hoặc người nhà.
/// <c>null</c> = nhân viên phòng khám, thao tác được trên mọi hóa đơn. Tham số bắt buộc (không có giá trị
/// mặc định) để mọi nơi gọi buộc phải nêu rõ phạm vi thay vì vô tình bỏ qua kiểm tra.
/// </param>
public record CreatePaymentRequestCommand(Guid InvoiceId, PaymentGateway Gateway, Guid? RestrictToUserId) : IRequest<PaymentTransactionDto>;

/// <summary>Tạo (hoặc tái sử dụng) một yêu cầu thanh toán cho hóa đơn qua cổng chỉ định.</summary>
public class CreatePaymentRequestHandler(
    IInvoiceRepository invoiceRepository,
    IPaymentTransactionRepository paymentTransactionRepository,
    IPatientRepository patientRepository,
    IUnitOfWork unitOfWork,
    IPaymentGatewayResolver gatewayResolver,
    IConfiguration configuration) : IRequestHandler<CreatePaymentRequestCommand, PaymentTransactionDto>
{
    public async Task<PaymentTransactionDto> Handle(CreatePaymentRequestCommand command, CancellationToken ct)
    {
        var invoice = await invoiceRepository.GetByIdWithAppointmentAndPatientAsync(command.InvoiceId, ct)
            ?? throw new NotFoundException("Không tìm thấy hóa đơn.");

        if (command.RestrictToUserId is Guid requesterId)
            await EnsureInvoiceBelongsToUserAsync(invoice, requesterId, patientRepository, ct);

        if (invoice.Status == PaymentStatus.Paid)
            throw new ConflictException("Hóa đơn này đã được thanh toán.");

        // Tái sử dụng giao dịch Pending còn hiệu lực (nếu có) thay vì tạo link mới mỗi lần mở lại màn hình.
        var existing = await paymentTransactionRepository.GetLatestPendingAsync(command.InvoiceId, command.Gateway, ct);
        if (existing is not null)
            return ToDto(existing);

        var gatewaySvc = gatewayResolver.Resolve(command.Gateway);
        var patientName = invoice.Appointment.Patient.FullName;
        var orderCodeHint = $"{invoice.InvoiceNumber}-{Guid.NewGuid():N}"[..24];

        // Return/cancel URL đưa người dùng quay lại app sau khi thanh toán trên trang PayOS — CHỈ ảnh hưởng
        // trải nghiệm redirect trên trình duyệt, không phải kênh cập nhật trạng thái (đó là webhook, xử lý riêng
        // ở HandlePaymentWebhookHandler). Không được để null: PayOSGatewayService sẽ fallback sang BaseUrl của PayOS API.
        var frontendBase = (configuration["FrontendBaseUrl"] ?? "http://localhost:3000").TrimEnd('/');
        var linkResult = await gatewaySvc.CreatePaymentLinkAsync(new CreatePaymentLinkRequest(
            InvoiceId: invoice.Id,
            OrderCode: orderCodeHint,
            Amount: invoice.DepositAmount,
            Description: $"Thanh toan HD {invoice.InvoiceNumber}",
            BuyerName: patientName,
            ReturnUrl: $"{frontendBase}/payment-result?invoiceId={invoice.Id}&status=success",
            CancelUrl: $"{frontendBase}/payment-result?invoiceId={invoice.Id}&status=cancel"), ct);

        // Kiểm tra lại lần nữa NGAY TRƯỚC khi insert: lệnh gọi PayOS ở trên có độ trễ mạng đáng kể, đủ để một
        // request khác (2 tab, hoặc React StrictMode gọi effect 2 lần ở dev) lọt qua nhánh "chưa có Pending" phía
        // trên và đã insert xong trong lúc ta đang chờ PayOS trả lời — thu hẹp cửa sổ race, không để tạo 2 giao dịch.
        var raceWinner = await paymentTransactionRepository.GetLatestPendingAsync(command.InvoiceId, command.Gateway, ct);
        if (raceWinner is not null)
            return ToDto(raceWinner);

        var transaction = PaymentTransaction.Create(
            invoice.Id, command.Gateway, linkResult.GatewayOrderCode, invoice.DepositAmount,
            linkResult.CheckoutUrl, linkResult.QrCode, linkResult.RawResponsePayload, linkResult.ExpiresAt);

        paymentTransactionRepository.Add(transaction);
        await unitOfWork.SaveChangesAsync(ct);

        return ToDto(transaction);
    }
}
