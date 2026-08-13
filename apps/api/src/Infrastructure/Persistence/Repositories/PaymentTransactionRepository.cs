using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Infrastructure.Persistence.Repositories;

public class PaymentTransactionRepository(AppDbContext db) : IPaymentTransactionRepository
{
    public Task<PaymentTransaction?> GetLatestPendingAsync(Guid invoiceId, PaymentGateway gateway, CancellationToken ct = default) =>
        db.PaymentTransactions
            .Where(t => t.InvoiceId == invoiceId && t.Gateway == gateway && t.Status == TransactionStatus.Pending
                        && (t.ExpiresAt == null || t.ExpiresAt > DateTimeOffset.UtcNow))
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<PaymentTransaction>> GetPendingByInvoiceIdAsync(Guid invoiceId, CancellationToken ct = default) =>
        await db.PaymentTransactions
            .Where(t => t.InvoiceId == invoiceId && t.Status == TransactionStatus.Pending)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);

    public Task<PaymentTransaction?> GetLatestByInvoiceIdAsync(Guid invoiceId, CancellationToken ct = default) =>
        db.PaymentTransactions
            .Where(t => t.InvoiceId == invoiceId)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(ct);

    public Task<PaymentTransaction?> GetByIdWithInvoiceAndAppointmentAsync(Guid id, CancellationToken ct = default) =>
        db.PaymentTransactions
            .Include(t => t.Invoice).ThenInclude(i => i.Appointment)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<PaymentTransaction?> GetByGatewayOrderCodeWithInvoiceAndAppointmentAsync(
        PaymentGateway gateway, string gatewayOrderCode, CancellationToken ct = default) =>
        db.PaymentTransactions
            .Include(t => t.Invoice).ThenInclude(i => i.Appointment)
            .FirstOrDefaultAsync(t => t.Gateway == gateway && t.GatewayOrderCode == gatewayOrderCode, ct);

    public void Add(PaymentTransaction transaction) => db.PaymentTransactions.Add(transaction);
}
