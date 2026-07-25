using DentalClinic.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.API.Infrastructure.Persistence.Configurations;

public class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
{
    public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
    {
        builder.ToTable("PaymentTransactions");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.GatewayOrderCode).HasMaxLength(100).IsRequired();
        builder.Property(t => t.GatewayTransactionId).HasMaxLength(100);
        builder.Property(t => t.CheckoutUrl).HasMaxLength(2000);
        builder.Property(t => t.QrCode).HasMaxLength(2000);
        builder.Property(t => t.Amount).HasPrecision(18, 2);
        builder.Property(t => t.FailureReason).HasMaxLength(500);

        builder.Property(t => t.RawCreateResponsePayload).HasColumnType("jsonb");
        builder.Property(t => t.RawWebhookPayload).HasColumnType("jsonb");

        builder.HasIndex(t => new { t.Gateway, t.GatewayOrderCode }).IsUnique();
        builder.HasIndex(t => t.InvoiceId);

        builder.HasOne(t => t.Invoice)
            .WithMany(i => i.PaymentTransactions)
            .HasForeignKey(t => t.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
