namespace DentalClinic.API.Application.DTOs.Payments;

public record CreatePaymentRequestRequest(string? Gateway);

public record PaymentTransactionDto(
    Guid Id,
    Guid InvoiceId,
    string Gateway,
    string Status,
    string GatewayOrderCode,
    decimal Amount,
    string? CheckoutUrl,
    string? QrCode,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ExpiresAt);

public record PaymentStatusDto(
    Guid InvoiceId,
    string InvoiceStatus,
    PaymentTransactionDto? LatestTransaction);
