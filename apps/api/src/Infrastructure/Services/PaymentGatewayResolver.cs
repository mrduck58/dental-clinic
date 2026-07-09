using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Services;

namespace DentalClinic.API.Infrastructure.Services;

public class PaymentGatewayResolver(IEnumerable<IPaymentGatewayService> gateways) : IPaymentGatewayResolver
{
    public IPaymentGatewayService Resolve(PaymentGateway gateway) =>
        gateways.FirstOrDefault(g => g.Gateway == gateway)
            ?? throw new ValidationException($"Cổng thanh toán {gateway} chưa được hỗ trợ.");
}
