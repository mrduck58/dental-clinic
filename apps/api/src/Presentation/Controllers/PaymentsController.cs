using DentalClinic.API.Application.UseCases.Invoices;
using DentalClinic.API.Application.UseCases.Payments;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.API.Presentation.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController(
    PaymentHandler paymentHandler,
    InvoiceHandler invoiceHandler,
    IPatientRepository patientRepository,
    ICurrentUserService currentUser) : ControllerBase
{
    /// <summary>GET api/payments/invoices/my — Hóa đơn chưa thanh toán của bệnh nhân hiện tại (mobile app).</summary>
    [HttpGet("invoices/my")]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> GetMyInvoices(CancellationToken cancellationToken)
    {
        var patient = currentUser.UserId is Guid userId
            ? await patientRepository.GetByUserIdAsync(userId, cancellationToken)
            : null;
        if (patient is null) return Ok(new List<InvoiceDto>());

        var result = await invoiceHandler.GetPendingByPatientAsync(patient.Id, cancellationToken);
        return Ok(result);
    }

    /// <summary>POST api/payments/invoices/{invoiceId}/request — Tạo yêu cầu thanh toán (link/QR) cho hóa đơn.</summary>
    [HttpPost("invoices/{invoiceId:guid}/request")]
    [Authorize(Roles = "Patient,Staff,Admin,Owner")]
    public async Task<IActionResult> CreatePaymentRequest(
        Guid invoiceId, [FromBody] CreatePaymentRequestRequest request, CancellationToken cancellationToken)
    {
        var gateway = ParseGateway(request.Gateway);
        var result = await paymentHandler.CreatePaymentRequestAsync(invoiceId, gateway, cancellationToken);
        return Ok(result);
    }

    /// <summary>GET api/payments/invoices/{invoiceId}/status — Trạng thái thanh toán hiện tại (polling).</summary>
    [HttpGet("invoices/{invoiceId:guid}/status")]
    [Authorize(Roles = "Patient,Staff,Admin,Owner")]
    public async Task<IActionResult> GetStatus(Guid invoiceId, CancellationToken cancellationToken)
    {
        var result = await paymentHandler.GetStatusAsync(invoiceId, cancellationToken);
        return Ok(result);
    }

    /// <summary>POST api/payments/webhook/payos — Callback (IPN) từ PayOS. Không yêu cầu JWT, xác thực bằng signature.</summary>
    [HttpPost("webhook/payos")]
    [AllowAnonymous]
    public async Task<IActionResult> PayOSWebhook(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var raw = await reader.ReadToEndAsync(cancellationToken);

        var headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString());
        await paymentHandler.HandleWebhookAsync(PaymentGateway.PayOS, raw, headers, cancellationToken);

        return Ok();
    }

    private static PaymentGateway ParseGateway(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        null or "" or "payos" => PaymentGateway.PayOS,
        _ when Enum.TryParse<PaymentGateway>(value, ignoreCase: true, out var g) => g,
        _ => throw new ValidationException("Cổng thanh toán không hợp lệ.")
    };
}
