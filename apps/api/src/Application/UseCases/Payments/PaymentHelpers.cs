using DentalClinic.API.Application.DTOs.Payments;
using DentalClinic.API.Domain.Entities;

namespace DentalClinic.API.Application.UseCases.Payments;

internal static class PaymentHelpers
{
    public static PaymentTransactionDto ToDto(PaymentTransaction t) => new(
        t.Id, t.InvoiceId, t.Gateway.ToString(), t.Status.ToString(), t.GatewayOrderCode, t.Amount,
        t.CheckoutUrl, t.QrCode, t.CreatedAt, t.ExpiresAt);
}
