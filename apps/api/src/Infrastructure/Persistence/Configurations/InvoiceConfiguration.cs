using DentalClinic.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.API.Infrastructure.Persistence.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.InvoiceNumber)
            .HasMaxLength(30)
            .IsRequired();

        builder.HasIndex(i => i.InvoiceNumber)
            .IsUnique();

        builder.Property(i => i.Subtotal).HasPrecision(18, 2);
        builder.Property(i => i.Discount).HasPrecision(18, 2);
        builder.Property(i => i.TotalAmount).HasPrecision(18, 2);
        builder.Property(i => i.DepositAmount).HasPrecision(18, 2);

        builder.Ignore(i => i.RemainingAmount);

        builder.Property(i => i.Notes)
            .HasMaxLength(1000);

        builder.HasOne(i => i.Patient)
            .WithMany()
            .HasForeignKey(i => i.PatientId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(i => i.Appointment)
            .WithMany(a => a.Invoices)
            .HasForeignKey(i => i.AppointmentId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        // Hóa đơn thu phần còn lại trỏ về hóa đơn đặt cọc gốc (tự tham chiếu)
        builder.HasIndex(i => i.ParentInvoiceId);
        builder.HasOne<Invoice>()
            .WithMany()
            .HasForeignKey(i => i.ParentInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(i => i.Items)
            .WithOne(it => it.Invoice)
            .HasForeignKey(it => it.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
