using DentalClinic.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.API.Infrastructure.Persistence.Configurations;

public class InvoiceItemConfiguration : IEntityTypeConfiguration<InvoiceItem>
{
    public void Configure(EntityTypeBuilder<InvoiceItem> builder)
    {
        builder.ToTable("InvoiceItems");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Name)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(i => i.UnitPrice).HasPrecision(18, 2);
        builder.Property(i => i.AmountCollected).HasPrecision(18, 2);

        builder.Property(i => i.TreatmentPlanId);
        builder.HasIndex(i => i.TreatmentPlanId);

        builder.Property(i => i.TreatmentPlanItemId);
        builder.HasIndex(i => i.TreatmentPlanItemId);

        builder.HasOne(i => i.TreatmentPlanItem)
            .WithMany(tpi => tpi.InvoiceItems)
            .HasForeignKey(i => i.TreatmentPlanItemId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Ignore(i => i.LineTotal);
        builder.Ignore(i => i.LineRemaining);
    }
}
